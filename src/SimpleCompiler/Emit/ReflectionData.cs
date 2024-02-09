using System.Linq.Expressions;
using System.Reflection;
using SimpleCompiler.Runtime;

namespace SimpleCompiler;

internal static class ReflectionData
{
    public static readonly Type LuaValue = typeof(LuaValue);
    public static readonly MethodInfo LuaValue_AsFunction =
        LuaValue.GetMethod(nameof(Runtime.LuaValue.AsFunction), BindingFlags.Public | BindingFlags.Instance, [])
        ?? throw new Exception("Unable to find LuaValue.AsFunction()");

    public static readonly Type LuaOperations = typeof(LuaOperations);
    public static readonly MethodInfo LuaOperations_Add = StaticMethod(Runtime.LuaOperations.Add);
    public static readonly MethodInfo LuaOperations_BitwiseAnd = StaticMethod(Runtime.LuaOperations.BitwiseAnd);
    public static readonly MethodInfo LuaOperations_BitwiseNot = StaticMethod(Runtime.LuaOperations.BitwiseNot);
    public static readonly MethodInfo LuaOperations_BitwiseOr = StaticMethod(Runtime.LuaOperations.BitwiseOr);
    public static readonly MethodInfo LuaOperations_BitwiseXor = StaticMethod(Runtime.LuaOperations.BitwiseXor);
    public static readonly MethodInfo LuaOperations_BooleanNot = StaticMethod(Runtime.LuaOperations.BooleanNot);
    public static readonly MethodInfo LuaOperations_Call = StaticMethod(Runtime.LuaOperations.Call);
    public static readonly MethodInfo LuaOperations_Concatenate = StaticMethod(Runtime.LuaOperations.Concatenate);
    public static readonly MethodInfo LuaOperations_Divide = StaticMethod(Runtime.LuaOperations.Divide);
    public static readonly MethodInfo LuaOperations_Equals = StaticMethod(() => Runtime.LuaOperations.Equals(default, default));
    public static readonly MethodInfo LuaOperations_Exponentiate = StaticMethod(Runtime.LuaOperations.Exponentiate);
    public static readonly MethodInfo LuaOperations_GreaterThan = StaticMethod(Runtime.LuaOperations.GreaterThan);
    public static readonly MethodInfo LuaOperations_GreaterThanOrEqual = StaticMethod(Runtime.LuaOperations.GreaterThanOrEqual);
    public static readonly MethodInfo LuaOperations_IntegerDivide = StaticMethod(Runtime.LuaOperations.IntegerDivide);
    public static readonly MethodInfo LuaOperations_LessThan = StaticMethod(Runtime.LuaOperations.LessThan);
    public static readonly MethodInfo LuaOperations_LessThanOrEqual = StaticMethod(Runtime.LuaOperations.LessThanOrEqual);
    public static readonly MethodInfo LuaOperations_Modulo = StaticMethod(Runtime.LuaOperations.Modulo);
    public static readonly MethodInfo LuaOperations_Multiply = StaticMethod(Runtime.LuaOperations.Multiply);
    public static readonly MethodInfo LuaOperations_Negate = StaticMethod(Runtime.LuaOperations.Negate);
    public static readonly MethodInfo LuaOperations_NotEquals = StaticMethod(Runtime.LuaOperations.NotEquals);
    public static readonly MethodInfo LuaOperations_ShiftLeft = StaticMethod(Runtime.LuaOperations.ShiftLeft);
    public static readonly MethodInfo LuaOperations_ShiftRight = StaticMethod(Runtime.LuaOperations.ShiftRight);
    public static readonly MethodInfo LuaOperations_Subtract = StaticMethod(Runtime.LuaOperations.Subtract);

    public static readonly Type StockGlobals = typeof(StockGlobals);
    public static readonly FieldInfo StockGlobal_Print =
        StockGlobals.GetField(nameof(Runtime.StockGlobals.Print))
        ?? throw new Exception("Unable to find StockGlobals.Print");
    public static readonly FieldInfo StockGlobal_ToString =
        StockGlobals.GetField(nameof(Runtime.StockGlobals.ToString))
        ?? throw new Exception("Unable to find StockGlobals.ToString");

    public static readonly Type Console = typeof(Console);
    public static readonly MethodInfo Console_WriteLine_object =
        Console.GetMethod(nameof(System.Console.WriteLine), [typeof(object)])
        ?? throw new Exception("Unable to find Console.WriteLine(object)");

    public static readonly MethodInfo LuaFunction_Invoke =
        typeof(LuaFunction).GetMethod(nameof(LuaFunction.Invoke))
        ?? throw new InvalidOperationException("Cannot get LuaFunction.Invoke(...) method.");

    public static MethodInfo StaticMethod(Delegate @delegate) => @delegate.Method;
    public static MethodInfo StaticMethod<T>(Expression<Action> expression) =>
        ((MethodCallExpression) expression.Body).Method;
    public static MethodInfo StaticMethod<T>(Expression<Func<object>> expression) =>
        ((MethodCallExpression) expression.Body).Method;
    public static MethodInfo InstanceMethod<T>(Expression<Action<T>> expression) =>
        ((MethodCallExpression) expression.Body).Method;
    public static MethodInfo InstanceMethod<T>(Expression<Func<T, object>> expression) =>
        ((MethodCallExpression) expression.Body).Method;
}
