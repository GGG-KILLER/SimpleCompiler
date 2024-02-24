using System.Collections.Immutable;
using System.Diagnostics;
using SimpleCompiler.Helpers;

namespace SimpleCompiler.IR;

public static class GraphRewriter
{
    public static void ReplaceOperand(this IrGraph graph, Operand oldOperand, Operand newOperand)
    {
        foreach (var block in graph.BasicBlocks)
        {
            block.ReplaceOperand(oldOperand, newOperand);
        }
    }

    public static void ReplaceOperand(this BasicBlock block, Operand oldOperand, Operand newOperand)
    {
        foreach (var instruction in block.Instructions)
        {
            instruction.ReplaceOperand(oldOperand, newOperand);
        }
    }

    public static void ReplaceOperand(this Instruction instruction, Operand oldOperand, Operand newOperand)
    {
        if ((!instruction.IsAssignment || instruction.Name != oldOperand) && !instruction.Operands.Contains(oldOperand))
            return;

        switch (instruction.Kind)
        {
            case InstructionKind.DebugLocation:
            case InstructionKind.Branch:
                return;

            case InstructionKind.Assignment:
            {
                var assignment = CastHelper.FastCast<Assignment>(instruction);
                if (newOperand is NameValue newName)
                    assignment.Name = assignment.Name == oldOperand ? newName : assignment.Name;
                assignment.Value = assignment.Value == oldOperand ? newOperand : assignment.Value;
                break;
            }
            case InstructionKind.UnaryAssignment:
            {
                var assignment = CastHelper.FastCast<UnaryAssignment>(instruction);
                if (newOperand is NameValue newName)
                    assignment.Name = assignment.Name == oldOperand ? newName : assignment.Name;
                assignment.Operand = assignment.Operand == oldOperand ? newOperand : assignment.Operand;
                break;
            }
            case InstructionKind.BinaryAssignment:
            {
                var assignment = CastHelper.FastCast<BinaryAssignment>(instruction);
                if (newOperand is NameValue newName)
                    assignment.Name = assignment.Name == oldOperand ? newName : assignment.Name;
                assignment.Left = assignment.Left == oldOperand ? newOperand : assignment.Left;
                assignment.Right = assignment.Right == oldOperand ? newOperand : assignment.Right;
                break;
            }
            case InstructionKind.FunctionAssignment:
            {
                var assignment = CastHelper.FastCast<FunctionAssignment>(instruction);
                if (newOperand is NameValue newName)
                    assignment.Name = assignment.Name == oldOperand ? newName : assignment.Name;
                assignment.Callee = assignment.Callee == oldOperand ? newOperand : assignment.Callee;
                for (var idx = 0; idx < assignment.Arguments.Count; idx++)
                    assignment.Arguments[idx] = assignment.Arguments[idx] == oldOperand ? newOperand : assignment.Arguments[idx];
                break;
            }
            case InstructionKind.PhiAssignment:
            {
                var assignment = CastHelper.FastCast<PhiAssignment>(instruction);
                if (newOperand is NameValue newName)
                {
                    assignment.Name = assignment.Name == oldOperand ? newName : assignment.Name;
                    if (oldOperand is NameValue oldName)
                        assignment.Phi.ReplaceOperand(oldName, newName);
                }
                break;
            }
            case InstructionKind.ConditionalBranch:
            {
                var branch = CastHelper.FastCast<ConditionalBranch>(instruction);
                branch.Condition = newOperand;
                break;
            }

            default:
                throw new UnreachableException($"Cannot rewrite the operands of {instruction.Kind}");
        }
    }

    public static void ReplaceOperand(this Phi phi, NameValue oldOperand, NameValue newOperand)
    {
        if (!phi.Values.Any(x => x.Value == oldOperand))
            return;

        for (var idx = 0; idx < phi.Values.Count; idx++)
        {
            if (phi.Values[idx].Value == oldOperand)
                phi.Values[idx] = (phi.Values[idx].SourceBlockOrdinal, newOperand);
        }
    }

    public static void CleanupPhis(this IrGraph graph)
    {
        var redirects = new Dictionary<NameValue, NameValue>();
        var replaced = false;
        do
        {
            replaced = false;
            foreach (var block in graph.BasicBlocks)
            {
                var node = block.Instructions.First;
                while (node is not null)
                {
                    if (node.Value.Kind != InstructionKind.PhiAssignment)
                        goto next;

                    var instruction = CastHelper.FastCast<PhiAssignment>(node.Value);

                    // If node only has one distinct source, then we can just replace it by the actual source.
                    if (instruction.Phi.Values.DistinctBy(x => findFinalValue(x.Value)).Count() == 1)
                    {
                        // Cleanup:
                        //   1. Remove the instruction from the node
                        var next = node.Next;
                        block.Instructions.Remove(node);
                        node = next;

                        //   2. Rename the references to the old phi to the name that it had in its predecessor.
                        var (sourceBlockOrdinal, value) = instruction.Phi.Values.DistinctBy(x => x.Value).Single();
                        value = findFinalValue(value);
                        if (value is NameValue valueAsName)
                            redirects[instruction.Name] = valueAsName;

                        // Replace the name with the final value.
                        graph.ReplaceOperand(instruction.Name, value);
                        replaced = true;

                        continue;
                    }

                next:
                    node = node.Next;
                }
            }
        }
        while (replaced);

        Operand findFinalValue(Operand operand)
        {
            while (operand is NameValue name && redirects.TryGetValue(name, out var newName))
                operand = newName;
            return operand;
        }
    }
}
