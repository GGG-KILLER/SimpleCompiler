using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using SimpleCompiler.Helpers;
using SimpleCompiler.MIR;

namespace SimpleCompiler.LIR;

public sealed class MirLowerer : MirWalker
{
    private readonly List<Instruction> _instructions = [];

    private MirLowerer()
    {
    }

    public override void VisitVariableExpression(VariableExpression variable)
    {
        _instructions.Add(Instruction.PushVar(variable));
    }

    public override void VisitConstantExpression(ConstantExpression constant)
    {
        _instructions.Add(Instruction.PushCons(constant));
    }

    public override void VisitDiscardExpression(DiscardExpression discard)
    {
        throw ExceptionUtil.Unreachable;
    }

    public override void VisitUnaryOperationExpression(UnaryOperationExpression unaryOperation)
    {
        Visit(unaryOperation.Operand);
        _instructions.Add(unaryOperation.UnaryOperationKind switch
        {
            UnaryOperationKind.LogicalNegation => Instruction.Not(),
            UnaryOperationKind.BitwiseNegation => Instruction.BNot(),
            UnaryOperationKind.NumericalNegation => Instruction.Neg(),
            UnaryOperationKind.LengthOf => Instruction.Len(),
            _ => throw ExceptionUtil.Unreachable,
        });
    }

    public override void VisitBinaryOperationExpression(BinaryOperationExpression binaryOperation)
    {
        Visit(binaryOperation.Left);
        Visit(binaryOperation.Right);
        _instructions.Add(binaryOperation.BinaryOperationKind switch
        {
            BinaryOperationKind.Addition => Instruction.Add(),
            BinaryOperationKind.Subtraction => Instruction.Sub(),
            BinaryOperationKind.Multiplication => Instruction.Mul(),
            BinaryOperationKind.Division => Instruction.Div(),
            BinaryOperationKind.IntegerDivision => Instruction.IntDiv(),
            BinaryOperationKind.Modulo => Instruction.Mod(),
            BinaryOperationKind.Concatenation => Instruction.Concat(),
            BinaryOperationKind.Exponentiation => Instruction.Pow(),
            BinaryOperationKind.BooleanAnd => Instruction.BAnd(),
            BinaryOperationKind.BooleanOr => Instruction.BOr(),
            BinaryOperationKind.BitwiseAnd => Instruction.BAnd(),
            BinaryOperationKind.BitwiseOr => Instruction.BOr(),
            BinaryOperationKind.BitwiseXor => Instruction.Xor(),
            BinaryOperationKind.LeftShift => Instruction.LShift(),
            BinaryOperationKind.RightShift => Instruction.RShift(),
            BinaryOperationKind.Equals => Instruction.Eq(),
            BinaryOperationKind.NotEquals => Instruction.Neq(),
            BinaryOperationKind.LessThan => Instruction.Lt(),
            BinaryOperationKind.LessThanOrEquals => Instruction.Lte(),
            BinaryOperationKind.GreaterThan => Instruction.Gt(),
            BinaryOperationKind.GreaterThanOrEquals => Instruction.Gte(),
            _ => throw ExceptionUtil.Unreachable,
        });
    }

    public override void VisitEmptyStatement(EmptyStatement emptyStatement)
    {
    }

    public override void VisitExpressionStatement(ExpressionStatement expressionStatement)
    {
        Visit(expressionStatement.Expression);
        if (_instructions[^1] is not Debug)
            _instructions.Add(Instruction.Pop());
    }

    public override void VisitFunctionCallExpression(FunctionCallExpression functionCall)
    {
        if (functionCall.Callee is VariableExpression { VariableInfo.Name: "debug" })
        {
            Visit(functionCall.Arguments.Single());
            _instructions.Add(Instruction.Debug());
            return;
        }
        Visit(functionCall.Callee);
        _instructions.Add(Instruction.MkArgs(functionCall.Arguments.Count));
        var idx = 0;
        foreach (var arg in functionCall.Arguments)
        {
            _instructions.Add(Instruction.BeginArg(idx++));
            Visit(arg);
            _instructions.Add(Instruction.StoreArg());
        }
        _instructions.Add(Instruction.FCall());
    }

    public override void VisitAssignmentStatement(AssignmentStatement assignment)
    {
        foreach (var value in assignment.Values)
            Visit(value);
        foreach (var assignee in assignment.Assignees)
        {
            switch (assignee.Kind)
            {
                case MirKind.VariableExpression:
                    _instructions.Add(Instruction.StoreVar(Unsafe.As<VariableExpression>(assignee)));
                    break;
                case MirKind.DiscardExpression:
                    _instructions.Add(Instruction.Pop());
                    break;
                default:
                    throw ExceptionUtil.Unreachable;
            }
        }
    }

    public override void VisitStatementList(StatementList statementList)
    {
        foreach (var statement in statementList.Statements)
            Visit(statement);
    }

    public static IReadOnlyList<Instruction> Lower(MirNode node)
    {
        var visitor = new MirLowerer();
        visitor.Visit(node);
        return visitor._instructions.ToImmutableArray();
    }
}
