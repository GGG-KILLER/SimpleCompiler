using System.Collections.Immutable;
using System.Diagnostics;
using ClassEnumGen;
using SimpleCompiler.Helpers;
using SimpleCompiler.IR.Debug;

namespace SimpleCompiler.IR;

[ClassEnum]
[DebuggerDisplay($"{{{nameof(ToRepr)}(),nq}}")]
public abstract partial record Instruction(InstructionKind Kind)
{
    public static partial DebugLocation DebugLocation(SourceLocation location);

    public static partial Assignment Assignment(NameValue name, Operand operand);
    public static partial UnaryAssignment UnaryAssignment(NameValue? name, UnaryOperationKind operationKind, Operand operand);
    public static partial BinaryAssignment BinaryAssignment(NameValue? name, Operand left, BinaryOperationKind operatorKind, Operand right);
    public static partial FunctionAssignment FunctionAssignment(NameValue name, Operand callee, ImmutableArray<Operand> arguments);
    public static partial PhiAssignment PhiAssignment(NameValue name, Phi phi);

    public static partial Branch Branch(BranchTarget target);
    public static partial CondBranch CondBranch(Operand operand, BranchTarget ifTrue, BranchTarget ifFalse);

    public bool IsAssignment => Kind is InstructionKind.Assignment or InstructionKind.UnaryAssignment
                                     or InstructionKind.BinaryAssignment or InstructionKind.FunctionAssignment
                                     or InstructionKind.PhiAssignment;

    public NameValue? Assignee => Kind switch
    {
        InstructionKind.Assignment => CastHelper.FastCast<Assignment>(this).Name,
        InstructionKind.UnaryAssignment => CastHelper.FastCast<UnaryAssignment>(this).Name,
        InstructionKind.BinaryAssignment => CastHelper.FastCast<BinaryAssignment>(this).Name,
        InstructionKind.FunctionAssignment => CastHelper.FastCast<FunctionAssignment>(this).Name,
        InstructionKind.PhiAssignment => CastHelper.FastCast<PhiAssignment>(this).Name,
        _ => null
    };

    public IEnumerable<Operand> Operands
    {
        get
        {
            switch (Kind)
            {
                case InstructionKind.Assignment:
                    return [CastHelper.FastCast<Assignment>(this).Operand];
                case InstructionKind.UnaryAssignment:
                    return [CastHelper.FastCast<UnaryAssignment>(this).Operand];
                case InstructionKind.BinaryAssignment:
                {
                    var asg = CastHelper.FastCast<BinaryAssignment>(this);
                    return [asg.Left, asg.Right];
                }
                case InstructionKind.FunctionAssignment:
                {
                    var fna = CastHelper.FastCast<FunctionAssignment>(this);
                    return [fna.Callee, .. fna.Arguments];
                }
                case InstructionKind.PhiAssignment:
                    return CastHelper.FastCast<PhiAssignment>(this)
                                     .Phi
                                     .Values
                                     .Select(x => x.Value);
                case InstructionKind.CondBranch:
                    return [CastHelper.FastCast<CondBranch>(this).Operand];
                case InstructionKind.DebugLocation:
                case InstructionKind.Branch:
                    return [];
                default:
                    throw new NotImplementedException($"Operands not implemented for instruction {Kind}.");
            }
        }
    }

    public bool References(Operand operand)
    {
        switch (Kind)
        {
            case InstructionKind.Assignment:
                return CastHelper.FastCast<Assignment>(this).Operand == operand;
            case InstructionKind.UnaryAssignment:
                return CastHelper.FastCast<UnaryAssignment>(this).Operand == operand;
            case InstructionKind.BinaryAssignment:
            {
                var asg = CastHelper.FastCast<BinaryAssignment>(this);
                return asg.Left == operand || asg.Right == operand;
            }
            case InstructionKind.FunctionAssignment:
            {
                var fn = CastHelper.FastCast<FunctionAssignment>(this);
                return fn.Callee == operand || fn.Arguments.Contains(operand);
            }
            case InstructionKind.PhiAssignment:
                return CastHelper.FastCast<PhiAssignment>(this)
                                 .Phi
                                 .Values
                                 .Select(x => x.Value)
                                 .Contains(operand);
            case InstructionKind.CondBranch:
                return CastHelper.FastCast<CondBranch>(this).Operand == operand;
            default:
                return false;
        }
    }

    public string ToRepr()
    {
        return this switch
        {
            DebugLocation debug => $"# {debug.Location.Path}  {debug.Location.StartLine},{debug.Location.StartColumn}:{debug.Location.EndLine},{debug.Location.EndColumn}",

            Assignment asg => $"{asg.Name} = {asg.Operand}",
            UnaryAssignment una => $"{(una.Name != null ? $"{una.Name} = " : "")}{una.OperationKind switch
            {
                UnaryOperationKind.LogicalNegation => "not",
                UnaryOperationKind.BitwiseNegation => "bnot",
                UnaryOperationKind.NumericalNegation => "neg",
                UnaryOperationKind.LengthOf => "len",
                _ => throw new InvalidOperationException($"Invalid unary operation kind {una.OperationKind}"),
            }} {una.Operand}",
            BinaryAssignment bina => $"{(bina.Name != null ? $"{bina.Name} = " : "")}{bina.OperatorKind switch
            {
                BinaryOperationKind.Addition => "add",
                BinaryOperationKind.Subtraction => "sub",
                BinaryOperationKind.Multiplication => "mul",
                BinaryOperationKind.Division => "div",
                BinaryOperationKind.IntegerDivision => "idiv",
                BinaryOperationKind.Exponentiation => "pow",
                BinaryOperationKind.Modulo => "mod",
                BinaryOperationKind.Concatenation => "cat",
                BinaryOperationKind.BitwiseAnd => "band",
                BinaryOperationKind.BitwiseOr => "bor",
                BinaryOperationKind.BitwiseXor => "xor",
                BinaryOperationKind.LeftShift => "lsh",
                BinaryOperationKind.RightShift => "rsh",
                BinaryOperationKind.Equals => "eq",
                BinaryOperationKind.NotEquals => "neq",
                BinaryOperationKind.LessThan => "lt",
                BinaryOperationKind.LessThanOrEquals => "lte",
                BinaryOperationKind.GreaterThan => "gt",
                BinaryOperationKind.GreaterThanOrEquals => "gte",
                _ => throw new InvalidOperationException($"Invalid binary operation kind {bina.OperatorKind}"),
            }} {bina.Left}, {bina.Right}",
            FunctionAssignment fna => $"{(fna.Name != null ? $"{fna.Name} = " : "")}{fna.Callee}({string.Join(", ", fna.Arguments)})",
            PhiAssignment phi => $"{phi.Name} = {phi.Phi}",

            Branch br => $"br BB{br.Target.Block.Ordinal}",
            CondBranch cbr => $"br BB{cbr.IfTrue.Block.Ordinal} if {cbr.Operand} else BB{cbr.IfFalse.Block.Ordinal}",

            _ => throw new NotImplementedException($"ToRepr hasn't been implemented for {Kind}.")
        };
    }
}
