using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SimpleCompiler.Frontends.Lua;
using SimpleCompiler.Helpers;

namespace SimpleCompiler.IR;

public sealed partial class SsaRewriter
{
    public static void RewriteGraph(IrGraph source) =>
        new SsaRewriter(source).Rewrite();

    private readonly IrGraph _source;
    private readonly List<NameValue> _renamedNames = [];

    private SsaRewriter(IrGraph source)
    {
        _source = source;
    }

    public void Rewrite()
    {
        InsertPhis();
        RenameVariables();
        FixPhis();
        _source.CleanupPhis();

        foreach (var key in _renamedNames)
            _source.DebugData!.OriginalValueNames.Remove(key);
    }

    private void InsertPhis()
    {
        Span<byte> buffer = stackalloc byte[MathEx.RoundUpDivide(_source.BasicBlocks.Count, 8)];
        var worklist = new StackWorklist(_source, buffer);
        foreach (var block in _source.BasicBlocks) worklist.SetClean(block.Ordinal, false);

        var modified = false;
        do
        {
            modified = false;
            foreach (var block in _source.BasicBlocks)
            {
                if (worklist.IsClean(block.Ordinal))
                    continue;
                worklist.SetClean(block.Ordinal, true);

                var assigned = new HashSet<NameValue>();
                for (var node = block.Instructions.First; node is not null; node = node.Next)
                {
                    var instruction = node.Value;

                    // Add assignees
                    if (instruction.IsAssignment) assigned.Add(instruction.Name);

                    // Add phis for references
                    foreach (var name in instruction.Operands.OfType<NameValue>().Where(x => x.IsUnversioned).Except(assigned))
                    {
                        // Initialize the phi assignment
                        var values = new List<(int SourceBlockOrdinal, Operand Value)>(_source.Edges.GetPredecessors(block.Ordinal).Count());
                        var assignment = new PhiAssignment(name, new Phi(values));

                        // Insert phi and mark as assigned.
                        block.Instructions.AppendPhi(assignment);
                        assigned.Add(name);

                        // Fill in the phi
                        fillPhi(worklist, assignment, name);

                        modified = true;
                    }

                    // Fill in empty phis
                    if (instruction.Kind == InstructionKind.PhiAssignment)
                    {
                        var assignment = CastHelper.FastCast<PhiAssignment>(instruction);
                        if (assignment.Phi.Values.Count == 0)
                        {
                            fillPhi(worklist, assignment, assignment.Name);

                            modified = true;
                        }
                    }
                }

                void fillPhi(StackWorklist worklist, PhiAssignment assignment, NameValue name)
                {
                    assignment.Phi.Values.EnsureCapacity(_source.Edges.GetPredecessors(block.Ordinal).Count());
                    foreach (var predecessor in _source.GetPredecessors(block.Ordinal))
                    {
                        assignment.Phi.Values.Add((predecessor.Ordinal, name));

                        // Add phi to predecessor if it needs one.
                        if (!predecessor.Instructions.Any(x => x.IsAssignment && x.Name == name))
                        {
                            predecessor.Instructions.AppendPhi(new PhiAssignment(name, new Phi([])));

                            // Mark predecessor as dirty for phi-filling.
                            worklist.SetClean(predecessor.Ordinal, false);

                            modified = true;
                        }
                    }
                    assignment.Phi.Values.TrimExcess();
                }
            }
        }
        while (modified);
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
                            assignment.Value = versionOperand(assignment.Value);
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

                if (instruction.IsAssignment && instruction.Name is not null && instruction.Name.IsUnversioned)
                {
                    var newName = tracker.NewValue(instruction.Name.Name);
                    if (_source.DebugData is not null && _source.DebugData.OriginalValueNames.TryGetValue(instruction.Name, out var originalName))
                    {
                        _source.DebugData.OriginalValueNames[newName] = originalName;
                        _renamedNames.Add(instruction.Name);
                    }

                    instruction.Name = newName;
                    versions[instruction.Name!.Name] = instruction.Name;
                }
            }
        }

        Operand versionOperand(Operand operand) =>
            operand is NameValue name && name.IsUnversioned ? versions[name.Name] : operand;
    }

    private void FixPhis()
    {
        var phis = _source.BasicBlocks.SelectMany(x => x.Instructions.Nodes().Where(x => x.Value.Kind == InstructionKind.PhiAssignment).Select(y => (Block: x, Node: y))).ToArray();

        // Fix names for phis
        foreach (var (block, node) in phis)
        {
            var instruction = CastHelper.FastCast<PhiAssignment>(node.Value);

            // Fix Target
            var values = CollectionsMarshal.AsSpan(instruction.Phi.Values);
            for (var idx = 0; idx < values.Length; idx++)
            {
                ref var value = ref values[idx];
                if (value.Value is NameValue name)
                    value = value with { Value = findDefName(value.SourceBlockOrdinal, name.Name) };
            }
        }

        NameValue findDefName(int defOrdinal, string name)
        {
            foreach (var node in _source.BasicBlocks[defOrdinal].Instructions.Nodes().Reversed())
            {
                if (node.Value.IsAssignment && node.Value.Name.Name == name)
                    return node.Value.Name;
            }

            throw new UnreachableException($"Didn't expect to not find a definition for {name} in BB{defOrdinal}.");
        }
    }
}
