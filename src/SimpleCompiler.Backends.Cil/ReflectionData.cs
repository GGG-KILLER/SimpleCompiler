using System.Linq.Expressions;
using System.Reflection;
using SimpleCompiler.Runtime;

namespace SimpleCompiler;

internal static class ReflectionData
{
    public static readonly MethodInfo string_Equality = BinaryOperator<string, bool>((a, b) => a == b);
    public static readonly MethodInfo string_Inequality = BinaryOperator<string, bool>((a, b) => a != b);
    public static readonly MethodInfo string_Length = InstanceGetter<string, int>(x => x.Length);
    public static readonly MethodInfo string_Concat2 = StaticMethod(() => string.Concat("", ""));
    public static readonly MethodInfo Math_Pow = StaticMethod(Math.Pow);

    // TODO: Uncomment once kevin-montrose/Sigil#67 gets fixed.
    // public static readonly ConstructorInfo LuaValue_NilCtor = typeof(LuaValue).GetConstructor(Type.EmptyTypes)
    //     ?? throw new InvalidOperationException("Couldn't find nil constructor for LuaValue.");
    // public static readonly ConstructorInfo LuaValue_BoolCtor = typeof(LuaValue).GetConstructor([typeof(bool)])
    //     ?? throw new InvalidOperationException("Couldn't find bool constructor for LuaValue.");
    // public static readonly ConstructorInfo LuaValue_DoubleCtor = typeof(LuaValue).GetConstructor([typeof(double)])
    //     ?? throw new InvalidOperationException("Couldn't find double constructor for LuaValue.");
    // public static readonly ConstructorInfo LuaValue_LongCtor = typeof(LuaValue).GetConstructor([typeof(long)])
    //     ?? throw new InvalidOperationException("Couldn't find long constructor for LuaValue.");
    // public static readonly ConstructorInfo LuaValue_StringCtor = typeof(LuaValue).GetConstructor([typeof(string)])
    //     ?? throw new InvalidOperationException("Couldn't find string constructor for LuaValue.");
    // public static readonly ConstructorInfo LuaValue_FunctionCtor = typeof(LuaValue).GetConstructor([typeof(LuaFunction)])
    //     ?? throw new InvalidOperationException("Couldn't find LuaFunction constructor for LuaValue.");
    public static readonly MethodInfo LuaValue_Equality = BinaryOperator<LuaValue, bool>((a, b) => a == b);
    public static readonly MethodInfo LuaValue_Inequality = BinaryOperator<LuaValue, bool>((a, b) => a != b);
    public static readonly FieldInfo LuaValue_Nil = StaticField(() => LuaValue.Nil);
    public static readonly MethodInfo LuaValue_IsTruthy = InstanceGetter<LuaValue, bool>(x => x.IsTruthy);
    public static readonly FieldInfo LuaValue_Kind = InstanceField<LuaValue, ValueKind>(x => x.Kind);
    public static readonly MethodInfo LuaValue_AsBoolean = InstanceMethod<LuaValue>(x => x.AsBoolean());
    public static readonly MethodInfo LuaValue_AsLong = InstanceMethod<LuaValue>(x => x.AsLong());
    public static readonly MethodInfo LuaValue_AsDouble = InstanceMethod<LuaValue>(x => x.AsDouble());
    public static readonly MethodInfo LuaValue_AsFunction = InstanceMethod<LuaValue>(x => x.AsFunction());
    public static readonly MethodInfo LuaValue_AsString = InstanceMethod<LuaValue>(x => x.AsString());
    public static readonly MethodInfo LuaValue_ToInteger = InstanceMethod<LuaValue>(x => x.ToInteger());
    public static readonly MethodInfo LuaValue_ToNumber = InstanceMethod<LuaValue>(x => x.ToNumber());

    public static readonly MethodInfo LuaOperations_LessThan = StaticMethod(LuaOperations.LessThan);
    public static readonly MethodInfo LuaOperations_LessThanOrEqual = StaticMethod(LuaOperations.LessThanOrEqual);
    public static readonly MethodInfo LuaOperations_GreaterThan = StaticMethod(LuaOperations.GreaterThan);
    public static readonly MethodInfo LuaOperations_GreaterThanOrEqual = StaticMethod(LuaOperations.GreaterThanOrEqual);
    public static readonly MethodInfo LuaOperations_ThrowArithmeticError = StaticMethod(LuaOperations.ThrowArithmeticError);
    public static readonly MethodInfo LuaOperations_ThrowBitwiseError = StaticMethod(LuaOperations.ThrowBitwiseError);
    public static readonly MethodInfo LuaOperations_ThrowLengthError = StaticMethod(LuaOperations.ThrowLengthError);
    public static readonly MethodInfo LuaOperations_ThrowConcatError = StaticMethod(LuaOperations.ThrowConcatError);

    public static readonly FieldInfo Stdlib_AssertFunction = StaticField(() => Stdlib.AssertFunction);
    public static readonly FieldInfo Stdlib_TypeFunction = StaticField(() => Stdlib.TypeFunction);
    public static readonly FieldInfo Stdlib_PrintFunction = StaticField(() => Stdlib.PrintFunction);
    public static readonly FieldInfo Stdlib_ErrorFunction = StaticField(() => Stdlib.ErrorFunction);
    public static readonly FieldInfo Stdlib_ToStringFunction = StaticField(() => Stdlib.ToStringFunction);

    public static readonly ConstructorInfo ArgumentSpan_ctor =
        typeof(ReadOnlySpan<LuaValue>).GetConstructor([typeof(LuaValue[])])
        ?? throw new InvalidOperationException("Cannot get ReadOnlySpan<LuaValue>(LuaValue[]) ctor.");
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

    private static MethodInfo BinaryOperator<TInput, TOutput>(Expression<Func<TInput, TInput, TOutput>> expression) =>
        ((BinaryExpression) expression.Body).Method!;
}
