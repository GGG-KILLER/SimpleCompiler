using System.Collections.Immutable;
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
        var assigned = new HashSet<NameValue>();
        for (var blockOrdinal = 0; blockOrdinal < _source.BasicBlocks.Count; blockOrdinal++)
        {
            assigned.Clear();

            var block = _source.BasicBlocks[blockOrdinal];
            for (var i = 0; i < block.Instructions.Count; i++)
            {
                var instruction = block.Instructions[i];

                // Add assignees
                if (instruction.IsAssignment && instruction.Assignee is not null)
                    assigned.Add(instruction.Assignee);

                // Add phis for references
                foreach (var name in instruction.Operands.OfType<NameValue>().Where(x => x.IsUnversioned).Except(assigned))
                {
                    var index = block.Instructions.FindLastIndex(x => x.Kind == InstructionKind.PhiAssignment) + 1;

                    // Insert phi and mark as assigned.
                    block.Instructions.Insert(index, Instruction.PhiAssignment(name, new Phi([])));
                    assigned.Add(name);

                    // Since we added an instruction at the start, we need to increase the index so we go to the actual next instruction.
                    index++;
                }
            }
        }
    }

    private void FillInPhis()
    {
        var phis = _source.BasicBlocks.SelectMany(x => x.Instructions.OfType<PhiAssignment>().Select(y => (Block: x, Instruction: y)))
            .ToArray();

        foreach (var phi in phis)
        {
            var index = phi.Block.Instructions.IndexOf(phi.Instruction);
            var defs = findDefs(phi.Block.Ordinal, phi.Instruction.Name).Distinct();
            phi.Block.Instructions[index] = phi.Instruction with { Phi = new Phi(defs.Select(x => (x, phi.Instruction.Name)).ToImmutableArray()) };
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
            for (var i = 0; i < block.Instructions.Count; i++)
            {
                var instruction = block.Instructions[i];

                // We don't rewrite targets of phis in this pass.
                if (instruction.Kind != InstructionKind.PhiAssignment && instruction.Operands.OfType<NameValue>().Any(x => x.IsUnversioned))
                {
                    switch (instruction.Kind)
                    {
                        case InstructionKind.Assignment:
                        {
                            var assignment = CastHelper.FastCast<Assignment>(instruction);
                            block.Instructions[i] = assignment with { Operand = versions[((NameValue) assignment.Operand).Name] };
                            break;
                        }
                        case InstructionKind.UnaryAssignment:
                        {
                            var assignment = CastHelper.FastCast<UnaryAssignment>(instruction);
                            block.Instructions[i] = assignment with { Operand = versionOperand(assignment.Operand) };
                            break;
                        }
                        case InstructionKind.BinaryAssignment:
                        {
                            var assignment = CastHelper.FastCast<BinaryAssignment>(instruction);
                            block.Instructions[i] = assignment with
                            {
                                Left = versionOperand(assignment.Left),
                                Right = versionOperand(assignment.Right),
                            };
                            break;
                        }
                        case InstructionKind.FunctionAssignment:
                        {
                            var assignment = CastHelper.FastCast<FunctionAssignment>(instruction);
                            block.Instructions[i] = assignment with
                            {
                                Callee = versionOperand(assignment.Callee),
                                Arguments = assignment.Arguments.Select(versionOperand).ToImmutableArray(),
                            };
                            break;
                        }
                        case InstructionKind.CondBranch:
                        {
                            var branch = CastHelper.FastCast<CondBranch>(instruction);
                            block.Instructions[i] = branch with { Operand = versionOperand(branch.Operand) };
                            break;
                        }
                    }
                }

                if (instruction.IsAssignment && instruction.Assignee is not null && instruction.Assignee.IsUnversioned)
                {
                    block.Instructions[i] = instruction = instruction.Kind switch
                    {
                        InstructionKind.Assignment => CastHelper.FastCast<Assignment>(instruction) with { Name = tracker.NewValue(instruction.Assignee.Name) },
                        InstructionKind.UnaryAssignment => CastHelper.FastCast<UnaryAssignment>(instruction) with { Name = tracker.NewValue(instruction.Assignee.Name) },
                        InstructionKind.BinaryAssignment => CastHelper.FastCast<BinaryAssignment>(instruction) with { Name = tracker.NewValue(instruction.Assignee.Name) },
                        InstructionKind.FunctionAssignment => CastHelper.FastCast<FunctionAssignment>(instruction) with { Name = tracker.NewValue(instruction.Assignee.Name) },
                        InstructionKind.PhiAssignment => CastHelper.FastCast<PhiAssignment>(instruction) with { Name = tracker.NewValue(instruction.Assignee.Name) },
                        _ => throw new InvalidOperationException($"Unknown assignment instruction {instruction.Kind}.")
                    };

                    versions[instruction.Assignee!.Name] = instruction.Assignee;
                }
            }
        }

        Operand versionOperand(Operand operand) =>
            operand is NameValue name && name.IsUnversioned ? versions[name.Name] : operand;
    }

    private void FixAndCleanupPhis()
    {
        var phis = _source.BasicBlocks.SelectMany(x => x.Instructions.OfType<PhiAssignment>().Select(y => (Block: x, Instruction: y)))
            .ToArray();

        foreach (var phi in phis)
        {
            if (phi.Instruction.Phi.Values.Length == 1)
            {
                // Cleanup
                var index = phi.Block.Instructions.IndexOf(phi.Instruction);
                var (sourceBlockOrdinal, value) = phi.Instruction.Phi.Values[0];
                phi.Block.Instructions[index] = Instruction.Assignment(phi.Instruction.Name, findDefName(sourceBlockOrdinal, value.Name));
            }
            else
            {
                // Fix Target
                var index = phi.Block.Instructions.IndexOf(phi.Instruction);
                var values = phi.Instruction.Phi.Values.ToBuilder();
                for (var idx = 0; idx < values.Count; idx++)
                    values[idx] = (values[idx].SourceBlockOrdinal, findDefName(values[idx].SourceBlockOrdinal, values[idx].Value.Name));
                phi.Block.Instructions[index] = phi.Instruction with { Phi = new Phi(values.DrainToImmutable()) };
            }
        }

        NameValue findDefName(int defOrdinal, string name)
        {
            var instructions = _source.BasicBlocks[defOrdinal].Instructions;
            for (var idx = instructions.Count - 1; idx >= 0; idx--)
            {
                var instruction = instructions[idx];
                if (instruction.IsAssignment && instruction.Assignee?.Name == name)
                    return instruction.Assignee;
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
