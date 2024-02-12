using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Sigil;
using SimpleCompiler.IR;
using SimpleCompiler.Runtime;

namespace SimpleCompiler.Backends.Cil;

internal sealed class MethodCompiler(ModuleBuilder moduleBuilder, IrTree tree, Emit<Func<LuaValue, LuaValue>> method) : IrVisitor<MethodCompiler.EmitOptions, ResultKind>
{
    private readonly Scope _scope = new(moduleBuilder);
    private readonly SlotPool _slots = new(method);
    public Emit<Func<LuaValue, LuaValue>> Method => method;

    public static MethodCompiler Create(ModuleBuilder moduleBuilder, TypeBuilder typeBuilder, IrTree tree, string name)
    {
        return new MethodCompiler(
            moduleBuilder,
            tree,
            Emit<Func<LuaValue, LuaValue>>.BuildStaticMethod(
                typeBuilder,
                name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static)
        );
    }

    public void AddNilReturn()
    {
        method.NewObject<LuaValue>();
        method.Return();
    }

    public MethodBuilder CreateMethod() => method.CreateMethod(OptimizationOptions.All);

    public override ResultKind Visit(IrNode? node, EmitOptions options)
    {
        if (options.NeedsAddr() && !options.NeedsLuaValue())
            throw new ArgumentException("Cannot get the address of an unwrapped value.", nameof(options));

        var resultKind = base.Visit(node, options);
        Debug.Assert(resultKind is ResultKind.None or ResultKind.Any || !resultKind.IsMixed(), "Method has returned mixed result kind.");
        return resultKind;
    }

    public override ResultKind VisitStatementList(StatementList node, EmitOptions options)
    {
        foreach (var statement in node.Statements)
            Visit(statement, EmitOptions.None);

        return ResultKind.None;
    }

    public override ResultKind VisitAssignmentStatement(AssignmentStatement node, EmitOptions options)
    {
        foreach (var value in node.Values)
            Visit(value, EmitOptions.NeedsLuaValue);

        foreach (var assignee in node.Assignees.Reverse())
            MakeStore(assignee);

        return ResultKind.None;
    }

    public override ResultKind VisitExpressionStatement(ExpressionStatement node, EmitOptions options)
    {
        Visit(node.Expression, EmitOptions.None);
        method.Pop();

        return ResultKind.None;
    }

    public override ResultKind VisitEmptyStatement(EmptyStatement node, EmitOptions options) => ResultKind.None;

    public override ResultKind VisitFunctionCallExpression(FunctionCallExpression node, EmitOptions options)
    {
        // Load callee onto stack
        Visit(node.Callee, EmitOptions.NeedsLuaValueAddr);
        method.Call(ReflectionData.LuaValue_AsFunction);

        // Create arguments array
        method.LoadConstant(node.Arguments.Count);
        method.NewArray<LuaValue>();

        for (var i = 0; i < node.Arguments.Count; i++)
        {
            var argument = node.Arguments[i];

            // Duplicate array pointer
            method.Duplicate();
            // Load argument index into stack
            method.LoadConstant(i);

            // Load argument value onto stack
            Visit(argument, EmitOptions.NeedsLuaValue);

            // Store argument in array
            method.StoreElement<LuaValue>();
        }

        // Convert arguments into span
        method.NewObject(typeof(ReadOnlySpan<LuaValue>), [typeof(LuaValue[])]);

        // Use the call helper
        method.CallVirtual(ReflectionData.LuaFunction_Invoke);

        return ResultKind.Any;
    }

    public override ResultKind VisitBinaryOperationExpression(BinaryOperationExpression node, EmitOptions options)
    {
        var (leftDesired, rightDesired) = OperationFacts.DesiredOperands(node);
        var wrapOperands = node.BinaryOperationKind is BinaryOperationKind.Equals or BinaryOperationKind.NotEquals;

        var leftResult = Visit(node.Left, wrapOperands ? EmitOptions.NeedsLuaValueAddr : EmitOptions.None);
        if (!wrapOperands)
        {
            ConvertTo(leftResult, leftDesired);
        }

        var rightResult = Visit(node.Right, wrapOperands ? EmitOptions.NeedsLuaValueAddr : EmitOptions.None);
        if (!wrapOperands)
        {
            ConvertTo(rightResult, rightDesired);
        }

        switch (node.BinaryOperationKind)
        {
            case BinaryOperationKind.Addition:
                method.Add();
                break;
            case BinaryOperationKind.BitwiseAnd:
                method.And();
                break;
            case BinaryOperationKind.BitwiseOr:
                method.Or();
                break;
            case BinaryOperationKind.BitwiseXor:
                method.Xor();
                break;
            case BinaryOperationKind.Concatenation:
                method.Call(ReflectionData.string_Concat2);
                break;
            case BinaryOperationKind.Division:
            case BinaryOperationKind.IntegerDivision:
                method.Divide();
                break;
            case BinaryOperationKind.Exponentiation:
                method.Call(ReflectionData.Math_Pow);
                break;
            case BinaryOperationKind.Equals:
                method.Call(ReflectionData.LuaValue_Equals);
                break;
            case BinaryOperationKind.GreaterThan:
                method.CompareGreaterThan();
                break;
            case BinaryOperationKind.GreaterThanOrEquals:
                if (leftDesired == ResultKind.Int && rightDesired == ResultKind.Int)
                    method.CompareLessThan();
                else
                    method.UnsignedCompareLessThan();
                method.LoadConstant(false);
                method.CompareEqual();
                break;
            case BinaryOperationKind.LeftShift:
                method.ShiftLeft();
                break;
            case BinaryOperationKind.LessThan:
                method.CompareLessThan();
                break;
            case BinaryOperationKind.LessThanOrEquals:
                if (leftDesired == ResultKind.Int && rightDesired == ResultKind.Int)
                    method.CompareGreaterThan();
                else
                    method.UnsignedCompareGreaterThan();
                method.LoadConstant(false);
                method.CompareEqual();
                break;
            case BinaryOperationKind.Modulo:
                method.Remainder();
                break;
            case BinaryOperationKind.Multiplication:
                method.Multiply();
                break;
            case BinaryOperationKind.NotEquals:
                method.Call(ReflectionData.LuaValue_Equals);
                method.LoadConstant(false);
                method.CompareEqual();
                break;
            case BinaryOperationKind.RightShift:
                method.ShiftRight();
                break;
            case BinaryOperationKind.Subtraction:
                method.Subtract();
                break;
            case BinaryOperationKind.BooleanAnd:
                throw new NotImplementedException("Boolean and has not been implemented.");
            case BinaryOperationKind.BooleanOr:
                throw new NotImplementedException("Boolean or has not been implemented.");
        }

        if (options.NeedsLuaValue())
        {
            switch (node.ResultKind)
            {
                case ResultKind.Bool:
                    method.NewObject<LuaValue, bool>();
                    break;
                case ResultKind.Int:
                    method.NewObject<LuaValue, int>();
                    break;
                case ResultKind.Double:
                    method.NewObject<LuaValue, double>();
                    break;
                case ResultKind.Str:
                    method.NewObject<LuaValue, string>();
                    break;
                case ResultKind.Any:
                    // Result is already wrapped.
                    break;
            }

            if (options.NeedsAddr())
            {
                ConvertValueToAddr();
            }

            return ResultKind.Any;
        }

        return node.ResultKind;
    }

    public override ResultKind VisitUnaryOperationExpression(UnaryOperationExpression node, EmitOptions options)
    {
        var result = Visit(node.Operand, EmitOptions.None);

        switch (node.UnaryOperationKind)
        {
            case UnaryOperationKind.LogicalNegation:
                if (result == ResultKind.Bool)
                {
                    method.LoadConstant(false);
                    method.CompareEqual();
                }
                else if (result is ResultKind.Int or ResultKind.Double or ResultKind.Str or ResultKind.Func)
                {
                    method.Pop();
                    method.LoadConstant(false);
                }
                else if (result == ResultKind.Any)
                {
                    CallMethodOnStackLuaValue(ReflectionData.LuaValue_IsTruthy);
                    method.LoadConstant(false);
                    method.CompareEqual();
                }
                else
                {
                    throw new NotImplementedException($"Logical negation not implemented for result type {result}.");
                }
                if (options.NeedsLuaValue())
                {
                    method.NewObject<LuaValue, bool>();
                    if (options.NeedsAddr())
                        ConvertValueToAddr();
                }
                break;
            case UnaryOperationKind.BitwiseNegation:
                ConvertTo(result, ResultKind.Int);
                method.Not();
                if (options.NeedsLuaValue())
                {
                    method.NewObject<LuaValue, long>();
                    if (options.NeedsAddr())
                        ConvertValueToAddr();
                }
                break;
            case UnaryOperationKind.NumericalNegation:
            {
                var desired = OperationFacts.DesiredOperand(node);
                ConvertTo(result, desired);
                method.Negate();
                if (options.NeedsLuaValue())
                {
                    if (desired == ResultKind.Int)
                        method.NewObject<LuaValue, long>();
                    else
                        method.NewObject<LuaValue, double>();

                    if (options.NeedsAddr())
                        ConvertValueToAddr();
                }
                break;
            }
            case UnaryOperationKind.LengthOf:
                throw new NotImplementedException("Length operator has not been implemented.");
        }

        return options.NeedsLuaValue() ? ResultKind.Any : node.ResultKind;
    }

    public override ResultKind VisitDiscardExpression(DiscardExpression node, EmitOptions options) => ResultKind.None;

    public override ResultKind VisitConstantExpression(ConstantExpression node, EmitOptions options)
    {
        switch (node.ConstantKind)
        {
            case ConstantKind.Nil:
                if (options.NeedsLuaValue())
                {
                    method.NewObject<LuaValue>();
                    if (options.NeedsAddr())
                        method.LoadFieldAddress(ReflectionData.LuaValue_Nil);
                }
                else
                {
                    method.LoadNull();
                }
                break;
            case ConstantKind.Boolean:
                if (options.NeedsAddr())
                {
                    method.LoadFieldAddress(Unsafe.Unbox<bool>(node.Value!) ? ReflectionData.LuaValue_True : ReflectionData.LuaValue_False);
                }
                else
                {
                    method.LoadConstant(Unsafe.Unbox<bool>(node.Value!));
                    if (options.NeedsLuaValue())
                        method.NewObject<LuaValue, bool>();
                }
                break;
            case ConstantKind.Number:
                if (node.ResultKind == ResultKind.Int)
                {
                    method.LoadConstant(Unsafe.Unbox<long>(node.Value!));
                    if (options.NeedsLuaValue())
                    {
                        method.NewObject<LuaValue, long>();
                        if (options.NeedsAddr())
                            ConvertValueToAddr();
                    }
                }
                else
                {
                    method.LoadConstant(Unsafe.Unbox<double>(node.Value!));
                    if (options.NeedsLuaValue())
                    {
                        method.NewObject<LuaValue, double>();
                        if (options.NeedsAddr())
                            ConvertValueToAddr();
                    }
                }
                break;
            case ConstantKind.String:
                method.LoadConstant(Unsafe.As<string>(node.Value));
                if (options.NeedsLuaValue())
                {
                    method.NewObject<LuaValue, string>();
                    if (options.NeedsAddr())
                        ConvertValueToAddr();
                }
                break;
        }

        return options.NeedsLuaValue() ? ResultKind.Any : node.ResultKind;
    }

    public override ResultKind VisitVariableExpression(VariableExpression node, EmitOptions options)
    {
        if (node.VariableInfo == tree.GlobalScope.KnownGlobals.Assert)
        {
            if (options.NeedsAddr())
                method.LoadFieldAddress(ReflectionData.StockGlobal_Assert);
            else
                method.LoadField(ReflectionData.StockGlobal_Assert);
        }
        else if (node.VariableInfo == tree.GlobalScope.KnownGlobals.Type)
        {
            if (options.NeedsAddr())
                method.LoadFieldAddress(ReflectionData.StockGlobal_Type);
            else
                method.LoadField(ReflectionData.StockGlobal_Type);
        }
        else if (node.VariableInfo == tree.GlobalScope.KnownGlobals.Print)
        {
            if (options.NeedsAddr())
                method.LoadFieldAddress(ReflectionData.StockGlobal_Print);
            else
                method.LoadField(ReflectionData.StockGlobal_Print);
        }
        else if (node.VariableInfo == tree.GlobalScope.KnownGlobals.Error)
        {
            if (options.NeedsAddr())
                method.LoadFieldAddress(ReflectionData.StockGlobal_Error);
            else
                method.LoadField(ReflectionData.StockGlobal_Error);
        }
        else if (node.VariableInfo == tree.GlobalScope.KnownGlobals.Tostring)
        {
            if (options.NeedsAddr())
                method.LoadFieldAddress(ReflectionData.StockGlobal_ToString);
            else
                method.LoadField(ReflectionData.StockGlobal_ToString);
        }
        else
        {
            var local = _scope.GetLocal(node.VariableInfo);
            if (local is null)
            {
                if (options.NeedsAddr())
                    method.LoadFieldAddress(ReflectionData.LuaValue_Nil);
                else
                    method.NewObject<LuaValue>();
            }
            else
            {
                if (options.NeedsAddr())
                    method.LoadLocalAddress(local);
                else
                    method.LoadLocal(local);
            }
        }

        return ResultKind.Any;
    }

    private void MakeStore(Expression assignee)
    {
        switch (assignee.Kind)
        {
            case IrKind.DiscardExpression:
                method.Pop();
                break;

            case IrKind.VariableExpression:
            {
                var varNode = Unsafe.As<VariableExpression>(assignee);
                var local = _scope.GetOrCreateLocal(method, varNode.VariableInfo);
                method.StoreLocal(local);
                break;
            }

            default:
                throw new InvalidOperationException($"Cannot assign to {assignee.Kind}.");
        }
    }

    protected override ResultKind DefaultVisit(IrNode node, EmitOptions options) =>
        throw new NotImplementedException($"Emitting for {node.GetType()} has not been implemented.");

    private void CallMethodOnStackLuaValue(MethodInfo method)
    {
        if (method.DeclaringType != typeof(LuaValue) && !method.GetParameters().Any(param => param.ParameterType == typeof(LuaValue)))
            throw new InvalidOperationException("Attempted to call method that doesn't receive a LuaValue nor is a LuaValue instance method.");

        _slots.WithSlot((m, local) =>
        {
            m.StoreLocal(local);
            m.LoadLocalAddress(local);
            m.Call(method);
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ConvertValueToAddr()
    {
        _slots.WithSlot(static (m, slot) =>
        {
            m.StoreLocal(slot);
            m.LoadLocalAddress(slot);
        });
    }

    private bool ConvertTo(ResultKind source, ResultKind target)
    {
        if (source == target)
            return true;

        switch (target)
        {
            default:
                throw new ArgumentException("Invalid target kind.", nameof(target));

            case ResultKind.None:
                throw new InvalidOperationException("Cannot convert to none.");
            case ResultKind.Nil:
                throw new InvalidOperationException("Cannot convert to nil.");
            case ResultKind.Bool:
                if (source == ResultKind.Any)
                {
                    CallMethodOnStackLuaValue(ReflectionData.LuaValue_AsBoolean);
                    return true;
                }
                else
                {
                    method.LoadConstant("Value cannot be converted to a bool.");
                    method.NewObject<LuaException, string>();
                    method.Throw();
                    return false;
                }
            case ResultKind.Int:
                if (source == ResultKind.Double)
                {
                    method.Call(ReflectionData.LuaOperations_ToInt);
                    return true;
                }
                else if (source == ResultKind.Str)
                {
                    method.NewObject<LuaValue, string>();
                    CallMethodOnStackLuaValue(ReflectionData.LuaValue_ToInteger);
                    return true;
                }
                else if (source == ResultKind.Any)
                {
                    CallMethodOnStackLuaValue(ReflectionData.LuaValue_ToInteger);
                    return true;
                }
                else
                {
                    method.LoadConstant("Value cannot be converted to an integer.");
                    method.NewObject<LuaException, string>();
                    method.Throw();
                    return false;
                }
            case ResultKind.Double:
                if (source == ResultKind.Int)
                {
                    method.Convert<double>();
                    return true;
                }
                else if (source == ResultKind.Str)
                {
                    method.NewObject<LuaValue, string>();
                    CallMethodOnStackLuaValue(ReflectionData.LuaValue_ToNumber);
                    return true;
                }
                else if (source == ResultKind.Any)
                {
                    CallMethodOnStackLuaValue(ReflectionData.LuaValue_ToNumber);
                    return true;
                }
                else
                {
                    method.LoadConstant("Value cannot be converted to a number.");
                    method.NewObject<LuaException, string>();
                    method.Throw();
                    return false;
                }
            case ResultKind.Str:
                method.LoadConstant("Value is not a string.");
                method.NewObject<LuaException, string>();
                method.Throw();
                return false;
            case ResultKind.Any:
                switch (source)
                {
                    case ResultKind.None:
                        throw new ArgumentException("Cannot convert none to any.", nameof(source));
                    case ResultKind.Nil:
                        method.Pop();
                        method.NewObject<LuaValue>();
                        break;
                    case ResultKind.Bool:
                        method.NewObject<LuaValue, bool>();
                        break;
                    case ResultKind.Int:
                        method.NewObject<LuaValue, long>();
                        break;
                    case ResultKind.Double:
                        method.NewObject<LuaValue, double>();
                        break;
                    case ResultKind.Str:
                        method.NewObject<LuaValue, string>();
                        break;
                    case ResultKind.Func:
                        method.NewObject<LuaValue, LuaFunction>();
                        break;
                    case ResultKind.Any:
                        throw new UnreachableException();
                }
                return true;
        }
    }

    public enum EmitOptions
    {
        None = 0,

        NeedsLuaValue = 1 << 1,
        NeedsAddr = 1 << 2,

        NeedsLuaValueAddr = NeedsLuaValue | NeedsAddr
    }
}

internal static class EmitOptionsExtensions
{
    public static bool NeedsLuaValue(this MethodCompiler.EmitOptions options) => (options & MethodCompiler.EmitOptions.NeedsLuaValue) != 0;
    public static bool NeedsAddr(this MethodCompiler.EmitOptions options) => (options & MethodCompiler.EmitOptions.NeedsAddr) != 0;
}
