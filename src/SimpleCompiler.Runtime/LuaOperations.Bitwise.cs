using System.Runtime.CompilerServices;

namespace SimpleCompiler.Runtime;

public static partial class LuaOperations
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long BitwiseNot(LuaValue operand)
    {
        if (!operand.IsNumber)
        {
            LuaException.ThrowBitwise(KindToString(operand.Kind));
        }

        return ~operand.ToInteger();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long BitwiseAnd(LuaValue left, LuaValue right)
    {
        if (!left.IsNumber || !right.IsNumber)
        {
            if (left.IsNil || right.IsNil)
                LuaException.ThrowBitwise("nil");
            else if (left.IsBoolean || right.IsBoolean)
                LuaException.ThrowBitwise("boolean");
            else if (left.IsString || right.IsString)
                LuaException.ThrowBitwise("string");
        }

        return left.ToInteger() & right.ToInteger();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long BitwiseOr(LuaValue left, LuaValue right)
    {
        if (!left.IsNumber || !right.IsNumber)
        {
            if (left.IsNil || right.IsNil)
                LuaException.ThrowBitwise("nil");
            else if (left.IsBoolean || right.IsBoolean)
                LuaException.ThrowBitwise("boolean");
            else if (left.IsString || right.IsString)
                LuaException.ThrowBitwise("string");
        }

        return left.ToInteger() | right.ToInteger();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long BitwiseXor(LuaValue left, LuaValue right)
    {
        if (!left.IsNumber || !right.IsNumber)
        {
            if (left.IsNil || right.IsNil)
                LuaException.ThrowBitwise("nil");
            else if (left.IsBoolean || right.IsBoolean)
                LuaException.ThrowBitwise("boolean");
            else if (left.IsString || right.IsString)
                LuaException.ThrowBitwise("string");
        }

        return left.ToInteger() ^ right.ToInteger();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ShiftLeft(LuaValue left, LuaValue right)
    {
        if (!left.IsNumber || !right.IsNumber)
        {
            if (left.IsNil || right.IsNil)
                LuaException.ThrowBitwise("nil");
            else if (left.IsBoolean || right.IsBoolean)
                LuaException.ThrowBitwise("boolean");
            else if (left.IsString || right.IsString)
                LuaException.ThrowBitwise("string");
        }

        return left.ToInteger() << (int)right.ToInteger();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ShiftRight(LuaValue left, LuaValue right)
    {
        if (!left.IsNumber || !right.IsNumber)
        {
            if (left.IsNil || right.IsNil)
                LuaException.ThrowBitwise("nil");
            else if (left.IsBoolean || right.IsBoolean)
                LuaException.ThrowBitwise("boolean");
            else if (left.IsString || right.IsString)
                LuaException.ThrowBitwise("string");
        }

        return left.ToInteger() >> (int)right.ToInteger();
    }
}
