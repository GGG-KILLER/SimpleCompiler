namespace SimpleCompiler.Emit;

using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Sigil;
using Sigil.NonGeneric;
using SimpleCompiler.Runtime;
using SimpleCompiler.IR;
using Lokad.ILPack;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

internal sealed partial class Emitter : IrVisitor<ResultKind>
{
    private readonly ScopeStack _scopeStack;
    private readonly Stack<Emit> _methodStack = [];
    private Emit _method = null!;

    [MemberNotNull(nameof(_method))]
    public Emit PushMethod(Type returnType, string name, Type[] parameterTypes)
    {
        _method = Emit.BuildStaticMethod(
            returnType,
            parameterTypes,
            _programBuilder,
            name,
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
            strictBranchVerification: true);
        _methodStack.Push(_method);
        return _method;
    }

    public Emit? PopMethod()
    {
        _method = _methodStack.Pop();
        return _method;
    }

    public override ResultKind VisitStatementList(StatementList node)
    {
        ScopeStack.Scope? scope = null;
        if (node.ScopeInfo is not null)
            scope = _scopeStack.NewScope();

        foreach (var statement in node.Statements)
            Visit(statement);

        scope?.Dispose();

        return ResultKind.None;
    }

    public override ResultKind VisitAssignmentStatement(AssignmentStatement node)
    {
        foreach (var value in node.Values)
            Visit(value);

        foreach (var assignee in node.Assignees)
            MakeStore(assignee);

        return ResultKind.None;
    }

    public override ResultKind VisitExpressionStatement(ExpressionStatement node)
    {
        Visit(node.Expression);
        _method.Pop();

        return ResultKind.None;
    }

    public override ResultKind VisitEmptyStatement(EmptyStatement node) => ResultKind.None;

    public override ResultKind VisitFunctionCallExpression(FunctionCallExpression node)
    {
        // Load callee onto stack
        Visit(node.Callee);

        // Create arguments array
        _method.LoadConstant(node.Arguments.Count);
        _method.NewArray<LuaValue>();

        for (var i = 0; i < node.Arguments.Count; i++)
        {
            var argument = node.Arguments[i];

            // Duplicate array pointer
            _method.Duplicate();
            // Load argument index into stack
            _method.LoadConstant(i);

            // Load argument value onto stack
            Visit(argument);

            // Store argument in array
            _method.StoreElement<LuaValue>();
        }

        // Convert arguments into span
        _method.NewObject(typeof(ReadOnlySpan<LuaValue>), [typeof(LuaValue[])]);

        // Use the call helper
        _method.Call(ReflectionData.LuaOperations_Call);

        return node.ResultKind;
    }

    public override ResultKind VisitBinaryOperationExpression(BinaryOperationExpression node)
    {
        Visit(node.Left);
        Visit(node.Right);

        switch (node.BinaryOperationKind)
        {
            case BinaryOperationKind.Addition:
                _method.Call(ReflectionData.LuaOperations_Add);
                break;
            case BinaryOperationKind.BitwiseAnd:
                _method.Call(ReflectionData.LuaOperations_BitwiseAnd);
                break;
            case BinaryOperationKind.BitwiseOr:
                _method.Call(ReflectionData.LuaOperations_BitwiseOr);
                break;
            case BinaryOperationKind.BitwiseXor:
                _method.Call(ReflectionData.LuaOperations_BitwiseXor);
                break;
            case BinaryOperationKind.BooleanAnd:
                throw new NotImplementedException("Boolean and has not been implemented.");
            case BinaryOperationKind.BooleanOr:
                throw new NotImplementedException("Boolean or has not been implemented.");
            case BinaryOperationKind.Concatenation:
                _method.Call(ReflectionData.LuaOperations_Concatenate);
                break;
            case BinaryOperationKind.Division:
                _method.Call(ReflectionData.LuaOperations_Divide);
                break;
            case BinaryOperationKind.Equals:
                _method.Call(ReflectionData.LuaOperations_Equals);
                break;
            case BinaryOperationKind.Exponentiation:
                _method.Call(ReflectionData.LuaOperations_Exponentiate);
                break;
            case BinaryOperationKind.GreaterThan:
                _method.Call(ReflectionData.LuaOperations_GreaterThan);
                break;
            case BinaryOperationKind.GreaterThanOrEquals:
                _method.Call(ReflectionData.LuaOperations_GreaterThanOrEqual);
                break;
            case BinaryOperationKind.IntegerDivision:
                _method.Call(ReflectionData.LuaOperations_IntegerDivide);
                break;
            case BinaryOperationKind.LeftShift:
                _method.Call(ReflectionData.LuaOperations_ShiftLeft);
                break;
            case BinaryOperationKind.LessThan:
                _method.Call(ReflectionData.LuaOperations_LessThan);
                break;
            case BinaryOperationKind.LessThanOrEquals:
                _method.Call(ReflectionData.LuaOperations_LessThanOrEqual);
                break;
            case BinaryOperationKind.Modulo:
                _method.Call(ReflectionData.LuaOperations_Modulo);
                break;
            case BinaryOperationKind.Multiplication:
                _method.Call(ReflectionData.LuaOperations_Multiply);
                break;
            case BinaryOperationKind.NotEquals:
                _method.Call(ReflectionData.LuaOperations_NotEquals);
                break;
            case BinaryOperationKind.RightShift:
                _method.Call(ReflectionData.LuaOperations_ShiftRight);
                break;
            case BinaryOperationKind.Subtraction:
                _method.Call(ReflectionData.LuaOperations_Subtract);
                break;
        }
        return node.ResultKind;
    }

    public override ResultKind VisitUnaryOperationExpression(UnaryOperationExpression node)
    {
        Visit(node.Operand);
        switch (node.UnaryOperationKind)
        {
            case UnaryOperationKind.LogicalNegation:
                _method.Call(ReflectionData.LuaOperations_BooleanNot);
                break;
            case UnaryOperationKind.BitwiseNegation:
                _method.Call(ReflectionData.LuaOperations_BitwiseNot);
                break;
            case UnaryOperationKind.NumericalNegation:
                _method.Call(ReflectionData.LuaOperations_Negate);
                break;
            case UnaryOperationKind.LengthOf:
                throw new NotImplementedException("Length operator has not been implemented.");
        }

        return node.ResultKind;
    }

    public override ResultKind VisitDiscardExpression(DiscardExpression node) => ResultKind.None;

    public override ResultKind VisitConstantExpression(ConstantExpression node)
    {
        switch (node.ConstantKind)
        {
            case ConstantKind.Nil:
                _method.NewObject<LuaValue>();
                break;
            case ConstantKind.Boolean:
                _method.LoadConstant(Unsafe.Unbox<bool>(node.Value!));
                _method.NewObject<LuaValue, bool>();
                break;
            case ConstantKind.Number:
                if (node.ResultKind == ResultKind.Int)
                {
                    _method.LoadConstant(Unsafe.Unbox<long>(node.Value!));
                    _method.NewObject<LuaValue, long>();
                }
                else
                {
                    _method.LoadConstant(Unsafe.Unbox<double>(node.Value!));
                    _method.NewObject<LuaValue, double>();
                }
                break;
            case ConstantKind.String:
                _method.LoadConstant(Unsafe.As<string>(node.Value));
                _method.NewObject<LuaValue, string>();
                break;
        }

        return node.ResultKind;
    }

    public override ResultKind VisitVariableExpression(VariableExpression node)
    {
        if (node.VariableInfo == _tree.GlobalScope.KnownGlobals.Print)
        {
            _method.LoadField(ReflectionData.StockGlobal_Print);
        }
        else if (node.VariableInfo == _tree.GlobalScope.KnownGlobals.Tostring)
        {
            _method.LoadField(ReflectionData.StockGlobal_ToString);
        }
        else
        {
            var local = _scopeStack.Current.GetOrCreateLocal(_method, node.VariableInfo);
            _method.LoadLocal(local);
        }

        return node.ResultKind;
    }

    private void MakeStore(Expression assignee)
    {
        switch (assignee.Kind)
        {
            case IrKind.DiscardExpression:
                _method.Pop();
                break;

            case IrKind.VariableExpression:
            {
                var var = Unsafe.As<VariableExpression>(assignee);
                _method.StoreLocal(_scopeStack.Current.GetLocal(var.VariableInfo));
                break;
            }

            default:
                throw new InvalidOperationException($"Cannot assign to {assignee.Kind}.");
        }
    }

    protected override ResultKind DefaultVisit(IrNode node) => throw new NotImplementedException($"Emitting for {node.GetType()} has not been implemented.");
}
