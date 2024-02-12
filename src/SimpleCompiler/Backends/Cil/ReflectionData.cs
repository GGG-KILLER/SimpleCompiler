using System.Linq.Expressions;
using System.Reflection;
using SimpleCompiler.Runtime;

namespace SimpleCompiler;

internal static class ReflectionData
{
    public static readonly MethodInfo string_Concat2 = StaticMethod(() => string.Concat("", ""));
    public static readonly MethodInfo Math_Pow = StaticMethod(Math.Pow);

    public static readonly FieldInfo LuaValue_Nil = StaticField(() => LuaValue.Nil);
    public static readonly FieldInfo LuaValue_True = StaticField(() => LuaValue.True);
    public static readonly FieldInfo LuaValue_False = StaticField(() => LuaValue.False);
    public static readonly MethodInfo LuaValue_Equals = InstanceMethod<LuaValue>(x => x.Equals(default(LuaValue)));
    public static readonly MethodInfo LuaValue_IsTruthy = InstanceGetter<LuaValue, bool>(x => x.IsTruthy);
    public static readonly MethodInfo LuaValue_AsBoolean = InstanceMethod<LuaValue>(x => x.AsBoolean());
    public static readonly MethodInfo LuaValue_AsFunction = InstanceMethod<LuaValue>(x => x.AsFunction());
    public static readonly MethodInfo LuaValue_ToInteger = InstanceMethod<LuaValue>(x => x.ToInteger());
    public static readonly MethodInfo LuaValue_ToNumber = InstanceMethod<LuaValue>(x => x.ToNumber());

    public static readonly MethodInfo LuaOperations_ToInt = StaticMethod(LuaOperations.ToInt);

    public static readonly FieldInfo StockGlobal_Assert = StaticField(() => StockGlobals.Assert);
    public static readonly FieldInfo StockGlobal_Type = StaticField(() => StockGlobals.Type);
    public static readonly FieldInfo StockGlobal_Print = StaticField(() => StockGlobals.Print);
    public static readonly FieldInfo StockGlobal_Error = StaticField(() => StockGlobals.Error);
    public static readonly FieldInfo StockGlobal_ToString = StaticField(() => StockGlobals.ToString);

    public static readonly MethodInfo LuaFunction_Invoke =
        typeof(LuaFunction).GetMethod(nameof(LuaFunction.Invoke))
        ?? throw new InvalidOperationException("Cannot get LuaFunction.Invoke(...) method.");

    private static MethodInfo StaticMethod(Delegate @delegate) => @delegate.Method;
    private static MethodInfo StaticMethod(Expression<Action> expression) =>
        ((MethodCallExpression) expression.Body).Method;
    private static MethodInfo StaticGetter<T>(Expression<Func<T>> expression) =>
        ((PropertyInfo) ((MemberExpression) expression.Body).Member).GetMethod!;
    private static FieldInfo StaticField<T>(Expression<Func<T>> expression) =>
        (FieldInfo) ((MemberExpression) expression.Body).Member;
    private static MethodInfo InstanceMethod<T>(Expression<Action<T>> expression) =>
        ((MethodCallExpression) expression.Body).Method;
    private static FieldInfo InstanceField<T, TRes>(Expression<Func<T, TRes>> expression) =>
        (FieldInfo) ((MemberExpression) expression.Body).Member;
    private static MethodInfo InstanceGetter<T, TRes>(Expression<Func<T, TRes>> expression) =>
        ((PropertyInfo) ((MemberExpression) expression.Body).Member).GetMethod!;
}
