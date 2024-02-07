using System.Reflection;
using SimpleCompiler.Runtime;

namespace SimpleCompiler;

internal static class ReflectionData
{
    public static readonly Type LuaValue = typeof(LuaValue);
    public static readonly MethodInfo LuaValue_AsFunction =
        LuaValue.GetMethod(nameof(Runtime.LuaValue.AsFunction), BindingFlags.Public | BindingFlags.Instance, [])
        ?? throw new Exception("Unable to find LuaValue.AsFunction()");

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

    public static readonly ConstructorInfo ReadOnlySpan_LuaValue_ctor_LuaValueArr =
        typeof(ReadOnlySpan<LuaValue>).GetConstructor([typeof(LuaValue[])])!
        ?? throw new InvalidOperationException("Cannot get ReadOnlySpan<LuaValue> constructor.");
}
