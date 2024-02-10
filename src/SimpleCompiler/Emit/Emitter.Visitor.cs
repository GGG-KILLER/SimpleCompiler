
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using SimpleCompiler.Runtime;
using SimpleCompiler.IR;
using System.Diagnostics.CodeAnalysis;
using Sigil;

namespace SimpleCompiler.Emit;

internal sealed partial class Emitter : IrVisitor<Emitter.EmitOptions, ResultKind>
{
    private readonly Stack<MethodContext> _contextStack = [];
    private MethodContext _context = null!;

    [MemberNotNull(nameof(_context))]
    public MethodContext PushMethod(string name)
    {
        _context = new MethodContext(_moduleBuilder, Emit<Func<LuaValue, LuaValue>>.BuildStaticMethod(
            _programBuilder,
            name,
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static));
        _contextStack.Push(_context);
        return _context;
    }

    public MethodContext PopMethod()
    {
        _context = _contextStack.Pop();
        return _context;
    }

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
        _context.Method.Pop();

        return ResultKind.None;
    }

    public override ResultKind VisitEmptyStatement(EmptyStatement node, EmitOptions options) => ResultKind.None;

    public override ResultKind VisitFunctionCallExpression(FunctionCallExpression node, EmitOptions options)
    {
        // Load callee onto stack
        Visit(node.Callee, EmitOptions.NeedsLuaValue);

        // Create arguments array
        _context.Method.LoadConstant(node.Arguments.Count);
        _context.Method.NewArray<LuaValue>();

        for (var i = 0; i < node.Arguments.Count; i++)
        {
            var argument = node.Arguments[i];

            // Duplicate array pointer
            _context.Method.Duplicate();
            // Load argument index into stack
            _context.Method.LoadConstant(i);

            // Load argument value onto stack
            Visit(argument, EmitOptions.NeedsLuaValue);

            // Store argument in array
            _context.Method.StoreElement<LuaValue>();
        }

        // Convert arguments into span
        _context.Method.NewObject(typeof(ReadOnlySpan<LuaValue>), [typeof(LuaValue[])]);

        // Use the call helper
        _context.Method.Call(ReflectionData.LuaOperations_Call);

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
                _context.Method.Add();
                break;
            case BinaryOperationKind.BitwiseAnd:
                _context.Method.And();
                break;
            case BinaryOperationKind.BitwiseOr:
                _context.Method.Or();
                break;
            case BinaryOperationKind.BitwiseXor:
                _context.Method.Xor();
                break;
            case BinaryOperationKind.Concatenation:
                _context.Method.Call(ReflectionData.string_Concat2);
                break;
            case BinaryOperationKind.Division:
            case BinaryOperationKind.IntegerDivision:
                _context.Method.Divide();
                break;
            case BinaryOperationKind.Exponentiation:
                _context.Method.Call(ReflectionData.Math_Pow);
                break;
            case BinaryOperationKind.Equals:
                _context.Method.Call(ReflectionData.LuaValue_Equals);
                break;
            case BinaryOperationKind.GreaterThan:
                _context.Method.CompareGreaterThan();
                break;
            case BinaryOperationKind.GreaterThanOrEquals:
                if (leftDesired == ResultKind.Int && rightDesired == ResultKind.Int)
                    _context.Method.CompareLessThan();
                else
                    _context.Method.UnsignedCompareLessThan();
                _context.Method.LoadConstant(false);
                _context.Method.CompareEqual();
                break;
            case BinaryOperationKind.LeftShift:
                _context.Method.ShiftLeft();
                break;
            case BinaryOperationKind.LessThan:
                _context.Method.CompareLessThan();
                break;
            case BinaryOperationKind.LessThanOrEquals:
                if (leftDesired == ResultKind.Int && rightDesired == ResultKind.Int)
                    _context.Method.CompareGreaterThan();
                else
                    _context.Method.UnsignedCompareGreaterThan();
                _context.Method.LoadConstant(false);
                _context.Method.CompareEqual();
                break;
            case BinaryOperationKind.Modulo:
                _context.Method.Remainder();
                break;
            case BinaryOperationKind.Multiplication:
                _context.Method.Multiply();
                break;
            case BinaryOperationKind.NotEquals:
                _context.Method.Call(ReflectionData.LuaValue_Equals);
                _context.Method.LoadConstant(false);
                _context.Method.CompareEqual();
                break;
            case BinaryOperationKind.RightShift:
                _context.Method.ShiftRight();
                break;
            case BinaryOperationKind.Subtraction:
                _context.Method.Subtract();
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
                    _context.Method.NewObject<LuaValue, bool>();
                    break;
                case ResultKind.Int:
                    _context.Method.NewObject<LuaValue, int>();
                    break;
                case ResultKind.Double:
                    _context.Method.NewObject<LuaValue, double>();
                    break;
                case ResultKind.Str:
                    _context.Method.NewObject<LuaValue, string>();
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
                    _context.Method.LoadConstant(false);
                    _context.Method.CompareEqual();
                }
                else if (result is ResultKind.Int or ResultKind.Double or ResultKind.Str or ResultKind.Func)
                {
                    _context.Method.Pop();
                    _context.Method.LoadConstant(false);
                }
                else if (result == ResultKind.Any)
                {
                    CallMethodOnStackLuaValue(ReflectionData.LuaValue_IsTruthy);
                    _context.Method.LoadConstant(false);
                    _context.Method.CompareEqual();
                }
                else
                {
                    throw new NotImplementedException($"Logical negation not implemented for result type {result}.");
                }
                if (options.NeedsLuaValue())
                {
                    _context.Method.NewObject<LuaValue, bool>();
                    if (options.NeedsAddr())
                        ConvertValueToAddr();
                }
                break;
            case UnaryOperationKind.BitwiseNegation:
                ConvertTo(result, ResultKind.Int);
                _context.Method.Not();
                if (options.NeedsLuaValue())
                {
                    _context.Method.NewObject<LuaValue, long>();
                    if (options.NeedsAddr())
                        ConvertValueToAddr();
                }
                break;
            case UnaryOperationKind.NumericalNegation:
            {
                var desired = OperationFacts.DesiredOperand(node);
                ConvertTo(result, desired);
                _context.Method.Negate();
                if (options.NeedsLuaValue())
                {
                    if (desired == ResultKind.Int)
                        _context.Method.NewObject<LuaValue, long>();
                    else
                        _context.Method.NewObject<LuaValue, double>();

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
                    _context.Method.NewObject<LuaValue>();
                    if (options.NeedsAddr())
                        _context.Method.LoadFieldAddress(ReflectionData.LuaValue_Nil);
                }
                else
                {
                    _context.Method.LoadNull();
                }
                break;
            case ConstantKind.Boolean:
                if (options.NeedsAddr())
                {
                    _context.Method.LoadFieldAddress(Unsafe.Unbox<bool>(node.Value!) ? ReflectionData.LuaValue_True : ReflectionData.LuaValue_False);
                }
                else
                {
                    _context.Method.LoadConstant(Unsafe.Unbox<bool>(node.Value!));
                    if (options.NeedsLuaValue())
                        _context.Method.NewObject<LuaValue, bool>();
                }
                break;
            case ConstantKind.Number:
                if (node.ResultKind == ResultKind.Int)
                {
                    _context.Method.LoadConstant(Unsafe.Unbox<long>(node.Value!));
                    if (options.NeedsLuaValue())
                    {
                        _context.Method.NewObject<LuaValue, long>();
                        if (options.NeedsAddr())
                            ConvertValueToAddr();
                    }
                }
                else
                {
                    _context.Method.LoadConstant(Unsafe.Unbox<double>(node.Value!));
                    if (options.NeedsLuaValue())
                    {
                        _context.Method.NewObject<LuaValue, double>();
                        if (options.NeedsAddr())
                            ConvertValueToAddr();
                    }
                }
                break;
            case ConstantKind.String:
                _context.Method.LoadConstant(Unsafe.As<string>(node.Value));
                if (options.NeedsLuaValue())
                {
                    _context.Method.NewObject<LuaValue, string>();
                    if (options.NeedsAddr())
                        ConvertValueToAddr();
                }
                break;
        }

        return options.NeedsLuaValue() ? ResultKind.Any : node.ResultKind;
    }

    public override ResultKind VisitVariableExpression(VariableExpression node, EmitOptions options)
    {
        if (node.VariableInfo == _tree.GlobalScope.KnownGlobals.Print)
        {
            if (options.NeedsAddr())
                _context.Method.LoadFieldAddress(ReflectionData.StockGlobal_Print);
            else
                _context.Method.LoadField(ReflectionData.StockGlobal_Print);
        }
        else if (node.VariableInfo == _tree.GlobalScope.KnownGlobals.Tostring)
        {
            if (options.NeedsAddr())
                _context.Method.LoadFieldAddress(ReflectionData.StockGlobal_ToString);
            else
                _context.Method.LoadField(ReflectionData.StockGlobal_ToString);
        }
        else
        {
            var local = _context.Scope.GetLocal(node.VariableInfo);
            if (local is null)
            {
                if (options.NeedsAddr())
                    _context.Method.LoadFieldAddress(ReflectionData.LuaValue_Nil);
                else
                    _context.Method.NewObject<LuaValue>();
            }
            else
            {
                if (options.NeedsAddr())
                    _context.Method.LoadLocalAddress(local);
                else
                    _context.Method.LoadLocal(local);
            }
        }

        return ResultKind.Any;
    }

    private void MakeStore(Expression assignee)
    {
        switch (assignee.Kind)
        {
            case IrKind.DiscardExpression:
                _context.Method.Pop();
                break;

            case IrKind.VariableExpression:
            {
                var varNode = Unsafe.As<VariableExpression>(assignee);
                var local = _context.Scope.GetOrCreateLocal(_context.Method, varNode.VariableInfo);
                _context.Method.StoreLocal(local);
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

        _context.WithSlot((m, local) =>
        {
            m.StoreLocal(local);
            m.LoadLocalAddress(local);
            m.Call(method);
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ConvertValueToAddr()
    {
        _context.WithSlot(static (m, slot) =>
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
                    _context.Method.LoadConstant("Value cannot be converted to a bool.");
                    _context.Method.NewObject<LuaException, string>();
                    _context.Method.Throw();
                    return false;
                }
            case ResultKind.Int:
                if (source == ResultKind.Double)
                {
                    _context.Method.Call(ReflectionData.LuaOperations_ToInt);
                    return true;
                }
                else if (source == ResultKind.Str)
                {
                    _context.Method.NewObject<LuaValue, string>();
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
                    _context.Method.LoadConstant("Value cannot be converted to an integer.");
                    _context.Method.NewObject<LuaException, string>();
                    _context.Method.Throw();
                    return false;
                }
            case ResultKind.Double:
                if (source == ResultKind.Int)
                {
                    _context.Method.Convert<double>();
                    return true;
                }
                else if (source == ResultKind.Str)
                {
                    _context.Method.NewObject<LuaValue, string>();
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
                    _context.Method.LoadConstant("Value cannot be converted to a number.");
                    _context.Method.NewObject<LuaException, string>();
                    _context.Method.Throw();
                    return false;
                }
            case ResultKind.Str:
                _context.Method.LoadConstant("Value is not a string.");
                _context.Method.NewObject<LuaException, string>();
                _context.Method.Throw();
                return false;
            case ResultKind.Any:
                switch (source)
                {
                    case ResultKind.None:
                        throw new ArgumentException("Cannot convert none to any.", nameof(source));
                    case ResultKind.Nil:
                        _context.Method.Pop();
                        _context.Method.NewObject<LuaValue>();
                        break;
                    case ResultKind.Bool:
                        _context.Method.NewObject<LuaValue, bool>();
                        break;
                    case ResultKind.Int:
                        _context.Method.NewObject<LuaValue, long>();
                        break;
                    case ResultKind.Double:
                        _context.Method.NewObject<LuaValue, double>();
                        break;
                    case ResultKind.Str:
                        _context.Method.NewObject<LuaValue, string>();
                        break;
                    case ResultKind.Func:
                        _context.Method.NewObject<LuaValue, LuaFunction>();
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
    public static bool NeedsLuaValue(this Emitter.EmitOptions options) => (options & Emitter.EmitOptions.NeedsLuaValue) != 0;
    public static bool NeedsAddr(this Emitter.EmitOptions options) => (options & Emitter.EmitOptions.NeedsAddr) != 0;
}
