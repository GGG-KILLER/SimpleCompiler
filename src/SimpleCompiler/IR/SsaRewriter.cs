using System.Diagnostics;
using SimpleCompiler.Frontends.Lua;
using SimpleCompiler.Helpers;
using Tsu.Buffers;

namespace SimpleCompiler.IR;

public sealed partial class SsaRewriter
{
    public static void RewriteGraph(IrGraph source) =>
        new SsaRewriter(source).Rewrite();

    private readonly IrGraph _source;

    private SsaRewriter(IrGraph source)
    {
        _source = source;
    }

    public void Rewrite()
    {
        InsertPhis();
        FillInPhis();
        RenameVariables();
        FixAndCleanupPhis();
    }

    private void InsertPhis()
    {
        foreach (var block in _source.BasicBlocks)
        {
            var assigned = new HashSet<NameValue>();
            for (var node = block.Instructions.First; node is not null; node = node.Next)
            {
                var instruction = node.Value;

                // Add assignees
                if (instruction.IsAssignment && instruction.Assignee is not null)
                    assigned.Add(instruction.Assignee);

                // Add phis for references
                foreach (var name in instruction.Operands.OfType<NameValue>().Where(x => x.IsUnversioned).Except(assigned))
                {
                    // Insert phi and mark as assigned.
                    block.Instructions.AppendPhi(new PhiAssignment(name, new Phi([])));
                    assigned.Add(name);
                }
            }
        }
    }

    private void FillInPhis()
    {
        var phis = _source.BasicBlocks.SelectMany(x => x.Instructions.Nodes().Where(x => x.Value.Kind == InstructionKind.PhiAssignment).Select(y => (Block: x, Node: y)));

        foreach (var (block, node) in phis)
        {
            var instruction = CastHelper.FastCast<PhiAssignment>(node.Value); ;
            var defs = findDefs(block.Ordinal, instruction.Name).Distinct();
            instruction.Phi.Values.AddRange(defs.Select(x => (x, instruction.Name)));
        }

        IEnumerable<int> findDefs(int useBlockOrdinal, NameValue nameValue)
        {
            Span<byte> buffer = stackalloc byte[MathEx.RoundUpDivide(_source.BasicBlocks.Count, 8)];
            var worklist = new Worklist(buffer, _source);
            worklist.MarkAll(false);

            var result = new HashSet<int>();
            findDefsCore(useBlockOrdinal, nameValue, worklist, result);
            return result;
        }

        void findDefsCore(int blockOrdinal, NameValue nameValue, Worklist visited, HashSet<int> definitionBlockOrdinals)
        {
            foreach (var predecessor in _source.GetPredecessors(blockOrdinal))
            {
                if (visited[predecessor.Ordinal])
                    continue;

                if (predecessor.Instructions.Any(x => x.IsAssignment && x.Assignee == nameValue))
                {
                    visited[predecessor.Ordinal] = true;
                    definitionBlockOrdinals.Add(predecessor.Ordinal);
                }
                else
                {
                    visited[predecessor.Ordinal] = true;
                    findDefsCore(predecessor.Ordinal, nameValue, visited, definitionBlockOrdinals);
                }
            }
        }
    }

    private void RenameVariables()
    {
        var versions = new Dictionary<string, NameValue>();
        var tracker = new NameTracker();
        foreach (var block in _source.BasicBlocks)
        {
            foreach (var node in block.Instructions.Nodes())
            {
                var instruction = node.Value;

                // We don't rewrite targets of phis in this pass.
                if (instruction.Kind != InstructionKind.PhiAssignment && instruction.Operands.OfType<NameValue>().Any(x => x.IsUnversioned))
                {
                    switch (instruction.Kind)
                    {
                        case InstructionKind.Assignment:
                        {
                            var assignment = CastHelper.FastCast<Assignment>(instruction);
                            assignment.Operand = versionOperand(assignment.Operand);
                            break;
                        }
                        case InstructionKind.UnaryAssignment:
                        {
                            var assignment = CastHelper.FastCast<UnaryAssignment>(instruction);
                            assignment.Operand = versionOperand(assignment.Operand);
                            break;
                        }
                        case InstructionKind.BinaryAssignment:
                        {
                            var assignment = CastHelper.FastCast<BinaryAssignment>(instruction);
                            assignment.Left = versionOperand(assignment.Left);
                            assignment.Right = versionOperand(assignment.Right);
                            break;
                        }
                        case InstructionKind.FunctionAssignment:
                        {
                            var assignment = CastHelper.FastCast<FunctionAssignment>(instruction);
                            assignment.Callee = versionOperand(assignment.Callee);
                            for (var idx = 0; idx < assignment.Arguments.Count; idx++)
                                assignment.Arguments[idx] = versionOperand(assignment.Arguments[idx]);
                            break;
                        }
                        case InstructionKind.ConditionalBranch:
                        {
                            var branch = CastHelper.FastCast<ConditionalBranch>(instruction);
                            branch.Condition = versionOperand(branch.Condition);
                            break;
                        }
                    }
                }

                if (instruction.IsAssignment && instruction.Assignee is not null && instruction.Assignee.IsUnversioned)
                {
                    instruction.Assignee = tracker.NewValue(instruction.Assignee.Name);
                    versions[instruction.Assignee!.Name] = instruction.Assignee;
                }
            }
        }

        Operand versionOperand(Operand operand) =>
            operand is NameValue name && name.IsUnversioned ? versions[name.Name] : operand;
    }

    private void FixAndCleanupPhis()
    {
        var phis = _source.BasicBlocks.SelectMany(x => x.Instructions.Nodes().Where(x => x.Value.Kind == InstructionKind.PhiAssignment).Select(y => (Block: x, Node: y)));

        foreach (var (block, node) in phis)
        {
            var instruction = CastHelper.FastCast<PhiAssignment>(node.Value);
            if (instruction.Phi.Values.Count == 1)
            {
                // Cleanup
                var (sourceBlockOrdinal, value) = instruction.Phi.Values[0];
                node.Value = new Assignment(instruction.Name, findDefName(sourceBlockOrdinal, value.Name));
            }
            else
            {
                // Fix Target
                var values = instruction.Phi.Values;
                for (var idx = 0; idx < values.Count; idx++)
                    values[idx] = values[idx] with { Value = findDefName(values[idx].SourceBlockOrdinal, values[idx].Value.Name) };
            }
        }

        NameValue findDefName(int defOrdinal, string name)
        {
            foreach (var node in _source.BasicBlocks[defOrdinal].Instructions.Nodes().Reversed())
            {
                if (node.Value.IsAssignment && node.Value.Assignee?.Name == name)
                    return node.Value.Assignee;
            }

            throw new UnreachableException($"Didn't expect to not find a definition for {name} in BB{defOrdinal}.");
        }
    }

    private readonly ref struct Worklist(Span<byte> buffer, IrGraph graph)
    {
        private readonly Span<byte> _buffer = buffer;

        public bool this[int index]
        {
            get => BitVectorHelpers.GetByteVectorBitValue(_buffer, index);
            set => BitVectorHelpers.SetByteVectorBitValue(_buffer, index, value);
        }

        public void MarkAll(bool isClean) => _buffer.Fill(isClean ? byte.MaxValue : byte.MinValue);

        public void MarkPredecessors(int blockOrdinal, bool isClean)
        {
            foreach (var incomingEdge in graph.Edges.Where(x => x.TargetBlockOrdinal == blockOrdinal))
            {
                this[incomingEdge.SourceBlockOrdinal] = isClean;
            }
        }

        public void MarkSuccessors(int blockOrdinal, bool isClean)
        {
            foreach (var incomingEdge in graph.Edges.Where(x => x.SourceBlockOrdinal == blockOrdinal))
            {
                this[incomingEdge.TargetBlockOrdinal] = isClean;
            }
        }
    }
}
