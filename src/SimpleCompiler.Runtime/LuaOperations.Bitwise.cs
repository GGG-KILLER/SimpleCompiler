using System.Runtime.CompilerServices;

namespace SimpleCompiler.Runtime;

public static partial class LuaOperations
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue BitwiseAnd(LuaValue left, LuaValue right)
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

        return new LuaValue(left.ToInteger() & right.ToInteger());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue BitwiseOr(LuaValue left, LuaValue right)
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

        return new LuaValue(left.ToInteger() | right.ToInteger());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue BitwiseXor(LuaValue left, LuaValue right)
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

        return new LuaValue(left.ToInteger() ^ right.ToInteger());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue ShiftLeft(LuaValue left, LuaValue right)
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

        return new LuaValue(left.ToInteger() << (int)right.ToInteger());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue ShiftRight(LuaValue left, LuaValue right)
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

        return new LuaValue(left.ToInteger() >> (int)right.ToInteger());
    }
}