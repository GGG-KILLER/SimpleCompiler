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
        for (var i = 0; i < block.Instructions.Count; i++)
        {
            var instruction = block.Instructions[i];
            if ((!instruction.IsAssignment || instruction.Assignee != oldOperand) && !instruction.Operands.Contains(oldOperand))
                continue;

            block.Instructions[i] = instruction.ReplaceOperand(oldOperand, newOperand);
        }
    }

    public static Instruction ReplaceOperand(this Instruction instruction, Operand oldOperand, Operand newOperand)
    {
        if ((!instruction.IsAssignment || instruction.Assignee != oldOperand) && !instruction.Operands.Contains(oldOperand))
            return instruction;

        switch (instruction.Kind)
        {
            case InstructionKind.DebugLocation:
            case InstructionKind.Branch:
                return instruction;

            case InstructionKind.Assignment:
            {
                var assignment = CastHelper.FastCast<Assignment>(instruction);
                return assignment with
                {
                    Name = assignment.Name == oldOperand ? (NameValue) newOperand : assignment.Name,
                    Operand = assignment.Operand == oldOperand ? newOperand : assignment.Operand
                };
            }
            case InstructionKind.UnaryAssignment:
            {
                var assignment = CastHelper.FastCast<UnaryAssignment>(instruction);
                return assignment with
                {
                    Name = assignment.Name == oldOperand ? (NameValue) newOperand : assignment.Name,
                    Operand = assignment.Operand == oldOperand ? newOperand : assignment.Operand
                };
            }
            case InstructionKind.BinaryAssignment:
            {
                var assignment = CastHelper.FastCast<BinaryAssignment>(instruction);
                return assignment with
                {
                    Name = assignment.Name == oldOperand ? (NameValue) newOperand : assignment.Name,
                    Left = assignment.Left == oldOperand ? newOperand : assignment.Left,
                    Right = assignment.Right == oldOperand ? newOperand : assignment.Right
                };
            }
            case InstructionKind.FunctionAssignment:
            {
                var assignment = CastHelper.FastCast<FunctionAssignment>(instruction);
                return assignment with
                {
                    Name = assignment.Name == oldOperand ? (NameValue) newOperand : assignment.Name,
                    Callee = assignment.Callee == oldOperand ? newOperand : assignment.Callee,
                    Arguments = assignment.Arguments.Select(x => x == oldOperand ? newOperand : x).ToImmutableArray()
                };
            }
            case InstructionKind.PhiAssignment:
            {
                var assignment = CastHelper.FastCast<PhiAssignment>(instruction);
                return assignment with
                {
                    Name = assignment.Name == oldOperand ? (NameValue) newOperand : assignment.Name,
                    Phi = oldOperand is NameValue oldValue ? assignment.Phi.ReplaceOperand(oldValue, (NameValue) newOperand) : assignment.Phi,
                };
            }
            case InstructionKind.CondBranch:
            {
                var branch = CastHelper.FastCast<CondBranch>(instruction);
                return branch with { Operand = newOperand };
            }

            default:
                throw new UnreachableException($"Cannot rewrite the operands of {instruction.Kind}");
        }
    }

    public static Phi ReplaceOperand(this Phi phi, NameValue oldOperand, NameValue newOperand)
    {
        if (!phi.Values.Any(x => x.Value == oldOperand))
            return phi;

        var builder = phi.Values.ToBuilder();
        for (var idx = 0; idx < builder.Count; idx++)
        {
            if (builder[idx].Value == oldOperand)
                builder[idx] = (builder[idx].SourceBlockOrdinal, newOperand);
        }
        return new Phi(builder.DrainToImmutable());
    }
}
