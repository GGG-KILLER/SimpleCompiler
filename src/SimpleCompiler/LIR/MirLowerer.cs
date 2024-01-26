using System.Collections.Immutable;
using SimpleCompiler.Helpers;
using SimpleCompiler.MIR;

namespace SimpleCompiler.LIR;

public sealed class MirLowerer : MirWalker
{
    private readonly List<Instruction> _instructions = [];

    private MirLowerer()
    {
    }

    public override void VisitVariable(Variable variable)
    {
        _instructions.Add(Instruction.PushVar(variable));
    }

    public override void VisitConstant(Constant constant)
    {
        _instructions.Add(Instruction.PushCons(constant));
    }

    public override void VisitDiscard(Discard discard)
    {
        throw ExceptionUtil.Unreachable;
    }

    public override void VisitUnaryOperation(UnaryOperation unaryOperation)
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

    public override void VisitBinaryOperation(BinaryOperation binaryOperation)
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
        _instructions.Add(Instruction.Pop());
    }

    public override void VisitFunctionCall(FunctionCall functionCall)
    {
        if (functionCall.Callee is Variable { VariableInfo.Name: "debug" })
        {
            Visit(functionCall.Arguments.Single());
            _instructions.Add(Instruction.Debug());
            return;
        }
        Visit(functionCall.Callee);
        _instructions.Add(Instruction.MkArgs(functionCall.Arguments.Length));
        var idx = 0;
        foreach (var arg in functionCall.Arguments)
        {
            _instructions.Add(Instruction.BeginArg(idx++));
            Visit(arg);
            _instructions.Add(Instruction.StoreArg());
        }
        _instructions.Add(Instruction.FCall());
    }

    public override void VisitAssignment(Assignment assignment)
    {
        foreach (var value in assignment.Values)
            Visit(value);
        foreach (var assignee in assignment.Assignees)
        {
            switch (assignee)
            {
                case Variable v:
                    _instructions.Add(Instruction.StoreVar(v));
                    break;
                case Discard:
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
