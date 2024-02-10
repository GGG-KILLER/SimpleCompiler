using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SimpleCompiler.Runtime;

public static partial class LuaOperations
{
    private static string KindToString(ValueKind kind) =>
        kind switch
        {
            ValueKind.Nil => "nil",
            ValueKind.Boolean => "boolean",
            ValueKind.Long or ValueKind.Double => "number",
            ValueKind.String => "string",
            _ => throw new UnreachableException()
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue Concatenate(LuaValue left, LuaValue right)
    {
        if ((!left.IsNumber && !left.IsString) || (!right.IsNumber && !right.IsString))
        {
            if (left.IsNil || right.IsNil)
                LuaException.ThrowConcat("nil");
            else if (left.IsBoolean || right.IsBoolean)
                LuaException.ThrowConcat("boolean");
        }

        return new LuaValue(left.ToString() + right.ToString());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue BooleanNot(LuaValue operand) => new(!operand.IsTruthy);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue Call(LuaValue callee, ReadOnlySpan<LuaValue> args) => callee.AsFunction()(args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue CreateValue(object? value)
    {
        return value switch
        {
            true => new LuaValue(true),
            false => new LuaValue(false),
            null => new LuaValue(),

            LuaValue luaValue => luaValue,
            string str => new LuaValue(str),
            long i64 => new LuaValue(i64),
            double f64 => new LuaValue(f64),
            LuaFunction function => new LuaValue(function),

            _ => throw new InvalidOperationException($"Cannot convert from {value.GetType()} to a LuaValue."),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToInt(double value) =>
        (long) value == value
        ? (long) value
        : throw new LuaException("Number does not have an integer representation.");
}
