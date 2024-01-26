using System.Diagnostics.CodeAnalysis;

namespace SimpleCompiler.Runtime;

public sealed class LuaException(string? message) : Exception(message)
{
    [DoesNotReturn]
    public static void ThrowArithmetic(string valueType) =>
        throw new LuaException($"Attemp to perform arithmetic on a {valueType} value.");

    [DoesNotReturn]
    public static void ThrowBitwise(string valueType) =>
        throw new LuaException($"Attemp to perform bitwise operation on a {valueType} value.");

    [DoesNotReturn]
    public static void ThrowConcat(string valueType) =>
        throw new LuaException($"Attempt to concatenate a {valueType} value.");

    [DoesNotReturn]
    public static void ThrowCompare(string valueType) =>
        throw new LuaException($"Attempt to compare two {valueType} values.");

    [DoesNotReturn]
    public static void ThrowCompare(string valueTypeLeft, string valueTypeRight) =>
        throw new LuaException($"Attempt to compare {valueTypeLeft} with {valueTypeRight}.");
}
