using System.Diagnostics;
using SimpleCompiler.Helpers;
using SimpleCompiler.IR;

namespace SimpleCompiler.Optimizations;

public sealed class InliningAndFolding : IOptimizationPass
{
    public void Execute(IrGraph graph)
    {
        foreach (var block in graph.BasicBlocks)
        {
            var instructions = block.Instructions;
            var node = instructions.First;
            while (node is not null)
            {
                var instruction = node.Value;
                bool hasUses = false;

                if (instruction.IsAssignment)
                {
                    foreach (var use in graph.FindUses(instruction.Name))
                    {
                        if (use.IsAssignment && use.Name == instruction.Name)
                            continue;

                        hasUses = true;
                        break; // No more info to collect, abort here.
                    }
                }

                if (instruction.Kind == InstructionKind.Assignment)
                {
                    var assignment = CastHelper.FastCast<Assignment>(instruction);

                    if (hasUses)
                        graph.ReplaceOperand(assignment.Name, assignment.Value);

                    // Remove the instruction before we replace it.
                    var next = node.Next;
                    if (node.Previous?.Value is DebugLocation)
                        instructions.Remove(node.Previous);
                    instructions.Remove(node);
                    node = next; // We need to use the next from before removing otherwise it's lost.
                    continue; // Do not set next at the end.
                }
                else if (instruction.Kind == InstructionKind.UnaryAssignment)
                {
                    var assignment = CastHelper.FastCast<UnaryAssignment>(instruction);
                    if (assignment.Operand is not Constant operandConstant)
                        goto next;

                    if (hasUses)
                    {
                        if (Fold(assignment.OperationKind, operandConstant) is not { } foldedConstant)
                            goto next;
                        graph.ReplaceOperand(assignment.Name, foldedConstant);
                    }

                    // Remove the instruction before we replace it.
                    var next = node.Next;

                    if (node.Previous?.Value is DebugLocation)
                        instructions.Remove(node.Previous);
                    instructions.Remove(node);
                    node = next; // We need to use the next from before removing otherwise it's lost.
                    continue;
                }
                else if (instruction.Kind == InstructionKind.BinaryAssignment)
                {
                    var assignment = CastHelper.FastCast<BinaryAssignment>(instruction);
                    if (assignment.Left is not Constant leftConstant || assignment.Right is not Constant rightConstant)
                        goto next;

                    if (hasUses)
                    {
                        if (Fold(assignment.OperationKind, leftConstant, rightConstant) is not { } foldedConstant)
                            goto next;
                        graph.ReplaceOperand(assignment.Name, foldedConstant);
                    }

                    // Remove the instruction before we replace it.
                    var next = node.Next;

                    if (node.Previous?.Value is DebugLocation)
                        instructions.Remove(node.Previous);
                    instructions.Remove(node);
                    node = next; // We need to use the next from before removing otherwise it's lost.
                    continue;
                }

            next:
                node = node.Next;
            }
        }
    }

    private static Constant? Fold(UnaryOperationKind kind, Constant operandConstant)
    {
        if (operandConstant.Value is double val && (long) val == val)
            operandConstant = new Constant(ConstantKind.Number, (long) val);

        switch (kind)
        {
            case UnaryOperationKind.LogicalNegation:
                return operandConstant.Value is null or false ? Constant.True : Constant.False;
            case UnaryOperationKind.BitwiseNegation:
            {
                if (operandConstant.Value is long i64)
                    return new Constant(ConstantKind.Number, ~i64);
                else // Cannot inline, either float or not a number.
                    return null;
            }
            case UnaryOperationKind.NumericalNegation:
            {
                if (operandConstant.Value is long i64)
                    return new Constant(ConstantKind.Number, -i64);
                else if (operandConstant.Value is double f64)
                    return new Constant(ConstantKind.Number, -f64);
                else // Cannot inline, not a number.
                    return null;
            }
            case UnaryOperationKind.LengthOf:
            {
                if (operandConstant.Value is string str)
                    return new Constant(ConstantKind.Number, (long) str.Length);
                else // Cannot get the length of, not a string.
                    return null;
            }
            default:
                Debug.Assert(false, $"Unsupported unary operation kind {kind}");
                return null;
        }
    }

    private static Constant? Fold(BinaryOperationKind kind, Constant leftConstant, Constant rightConstant)
    {
        if (leftConstant.Value is double v1 && (long) v1 == v1)
            leftConstant = new Constant(ConstantKind.Number, (long) v1);
        if (rightConstant.Value is double v2 && (long) v2 == v2)
            rightConstant = new Constant(ConstantKind.Number, (long) v2);

        switch (kind)
        {
            case BinaryOperationKind.Addition:
            {
                if (leftConstant.Value is long li64)
                {
                    if (rightConstant.Value is long ri64)
                        return new Constant(ConstantKind.Number, li64 + ri64);
                    else if (rightConstant.Value is double rf64)
                        return new Constant(ConstantKind.Number, li64 + rf64);
                    else
                        return null;
                }
                else if (leftConstant.Value is double lf64)
                {
                    if (rightConstant.Value is long ri64)
                        return new Constant(ConstantKind.Number, lf64 + ri64);
                    else if (rightConstant.Value is double rf64)
                        return new Constant(ConstantKind.Number, lf64 + rf64);
                    else
                        return null;
                }
                else
                {
                    return null;
                }
            }
            case BinaryOperationKind.Subtraction:
            {
                if (leftConstant.Value is long li64)
                {
                    if (rightConstant.Value is long ri64)
                        return new Constant(ConstantKind.Number, li64 - ri64);
                    else if (rightConstant.Value is double rf64)
                        return new Constant(ConstantKind.Number, li64 - rf64);
                    else
                        return null;
                }
                else if (leftConstant.Value is double lf64)
                {
                    if (rightConstant.Value is long ri64)
                        return new Constant(ConstantKind.Number, lf64 - ri64);
                    else if (rightConstant.Value is double rf64)
                        return new Constant(ConstantKind.Number, lf64 - rf64);
                    else
                        return null;
                }
                else
                {
                    return null;
                }
            }
            case BinaryOperationKind.Multiplication:
            {
                if (leftConstant.Value is long li64)
                {
                    if (rightConstant.Value is long ri64)
                        return new Constant(ConstantKind.Number, li64 * ri64);
                    else if (rightConstant.Value is double rf64)
                        return new Constant(ConstantKind.Number, li64 * rf64);
                    else
                        return null;
                }
                else if (leftConstant.Value is double lf64)
                {
                    if (rightConstant.Value is long ri64)
                        return new Constant(ConstantKind.Number, lf64 * ri64);
                    else if (rightConstant.Value is double rf64)
                        return new Constant(ConstantKind.Number, lf64 * rf64);
                    else
                        return null;
                }
                else
                {
                    return null;
                }
            }
            case BinaryOperationKind.Division:
            {
                if (leftConstant.Value is long li64)
                {
                    if (rightConstant.Value is long ri64)
                        return new Constant(ConstantKind.Number, li64 / (double) ri64);
                    else if (rightConstant.Value is double rf64)
                        return new Constant(ConstantKind.Number, li64 / rf64);
                    else
                        return null;
                }
                else if (leftConstant.Value is double lf64)
                {
                    if (rightConstant.Value is long ri64)
                        return new Constant(ConstantKind.Number, lf64 / ri64);
                    else if (rightConstant.Value is double rf64)
                        return new Constant(ConstantKind.Number, lf64 / rf64);
                    else
                        return null;
                }
                else
                {
                    return null;
                }
            }
            case BinaryOperationKind.IntegerDivision:
            {
                if (leftConstant.Value is long li64)
                {
                    if (rightConstant.Value is long ri64)
                        return new Constant(ConstantKind.Number, li64 / ri64);
                    else if (rightConstant.Value is double rf64)
                        return new Constant(ConstantKind.Number, (long) (li64 / rf64));
                    else
                        return null;
                }
                else if (leftConstant.Value is double lf64)
                {
                    if (rightConstant.Value is long ri64)
                        return new Constant(ConstantKind.Number, (long) (lf64 / ri64));
                    else if (rightConstant.Value is double rf64)
                        return new Constant(ConstantKind.Number, (long) (lf64 / rf64));
                    else
                        return null;
                }
                else
                {
                    return null;
                }
            }
            case BinaryOperationKind.Exponentiation:
            {
                if (leftConstant.Value is long li64)
                {
                    if (rightConstant.Value is long ri64)
                        return new Constant(ConstantKind.Number, Math.Pow(li64, ri64));
                    else if (rightConstant.Value is double rf64)
                        return new Constant(ConstantKind.Number, Math.Pow(li64, rf64));
                    else
                        return null;
                }
                else if (leftConstant.Value is double lf64)
                {
                    if (rightConstant.Value is long ri64)
                        return new Constant(ConstantKind.Number, Math.Pow(lf64, ri64));
                    else if (rightConstant.Value is double rf64)
                        return new Constant(ConstantKind.Number, Math.Pow(lf64, rf64));
                    else
                        return null;
                }
                else
                {
                    return null;
                }
            }
            case BinaryOperationKind.Modulo:
            {
                if (leftConstant.Value is long li64)
                {
                    if (rightConstant.Value is long ri64)
                        return new Constant(ConstantKind.Number, li64 % ri64);
                    else if (rightConstant.Value is double rf64)
                        return new Constant(ConstantKind.Number, li64 % rf64);
                    else
                        return null;
                }
                else if (leftConstant.Value is double lf64)
                {
                    if (rightConstant.Value is long ri64)
                        return new Constant(ConstantKind.Number, lf64 % ri64);
                    else if (rightConstant.Value is double rf64)
                        return new Constant(ConstantKind.Number, lf64 % rf64);
                    else
                        return null;
                }
                else
                {
                    return null;
                }
            }
            case BinaryOperationKind.Concatenation:
            {
                if (leftConstant.Value is string lstr && rightConstant.Value is string rstr)
                    return new Constant(ConstantKind.String, lstr + rstr);
                else
                    return null;
            }
            case BinaryOperationKind.BitwiseAnd:
            {
                if (leftConstant.Value is long li64 && rightConstant.Value is long ri64)
                    return new Constant(ConstantKind.Number, li64 & ri64);
                else
                    return null;
            }
            case BinaryOperationKind.BitwiseOr:
            {
                if (leftConstant.Value is long li64 && rightConstant.Value is long ri64)
                    return new Constant(ConstantKind.Number, li64 | ri64);
                else
                    return null;
            }
            case BinaryOperationKind.BitwiseXor:
            {
                if (leftConstant.Value is long li64 && rightConstant.Value is long ri64)
                    return new Constant(ConstantKind.Number, li64 ^ ri64);
                else
                    return null;
            }
            case BinaryOperationKind.LeftShift:
            {
                if (leftConstant.Value is long li64 && rightConstant.Value is long ri64)
                    return new Constant(ConstantKind.Number, li64 << (int) ri64);
                else
                    return null;
            }
            case BinaryOperationKind.RightShift:
            {
                if (leftConstant.Value is long li64 && rightConstant.Value is long ri64)
                    return new Constant(ConstantKind.Number, li64 >> (int) ri64);
                else
                    return null;
            }
            case BinaryOperationKind.Equals:
                return Equals(leftConstant.Value, rightConstant.Value) ? Constant.True : Constant.False;
            case BinaryOperationKind.NotEquals:
                return Equals(leftConstant.Value, rightConstant.Value) ? Constant.False : Constant.True;
            case BinaryOperationKind.LessThan:
            case BinaryOperationKind.LessThanOrEquals:
            case BinaryOperationKind.GreaterThan:
            case BinaryOperationKind.GreaterThanOrEquals:
            // TODO: Implement
            default:
                Debug.Assert(false, $"Folding for {kind} not implemented");
                return null;
        }
    }
}
