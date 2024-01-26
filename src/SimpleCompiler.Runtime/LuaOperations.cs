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
}
