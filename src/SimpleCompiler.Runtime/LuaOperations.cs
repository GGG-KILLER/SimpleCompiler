using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SimpleCompiler.Runtime;

public static partial class LuaOperations
{
    public static string KindToString(ValueKind kind) =>
        kind switch
        {
            ValueKind.Nil => "nil",
            ValueKind.Boolean => "boolean",
            ValueKind.Long or ValueKind.Double => "number",
            ValueKind.String => "string",
            _ => throw new UnreachableException()
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToInt(double value) =>
        (long) value == value
        ? (long) value
        : throw new LuaException("Number does not have an integer representation.");

    public static long ThrowArithmeticError(ValueKind valueKind)
    {
        LuaException.ThrowArithmetic(KindToString(valueKind));
        return default;
    }

    public static long ThrowBitwiseError(ValueKind valueKind)
    {
        LuaException.ThrowBitwise(KindToString(valueKind));
        return default;
    }

    public static long ThrowLengthError(ValueKind valueKind)
    {
        LuaException.ThrowLength(KindToString(valueKind));
        return default;
    }

    public static string ThrowConcatError(ValueKind valueKind)
    {
        LuaException.ThrowConcat(KindToString(valueKind));
        return string.Empty;
    }

    public static bool ThrowCompareError(ValueKind valueKind)
    {
        LuaException.ThrowCompare(KindToString(valueKind));
        return false;
    }

    public static bool ThrowCompareError2(ValueKind leftKind, ValueKind rightKind)
    {
        LuaException.ThrowCompare(KindToString(leftKind), KindToString(rightKind));
        return false;
    }
}
