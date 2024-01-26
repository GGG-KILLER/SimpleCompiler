using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SimpleCompiler.Runtime;

public static partial class LuaOperations
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue Equals(LuaValue left, LuaValue right)
    {
        if (left.IsNil && right.IsNil)
            return LuaValue.True;
        else if (left.IsBoolean && right.IsBoolean && left.AsBoolean() == right.AsBoolean())
            return LuaValue.True;
        else if (left.IsNumber && right.IsNumber && left.ToNumber() == right.ToNumber())
            return LuaValue.True;
        else if (left.IsString && right.IsString && left.AsString() == right.AsString())
            return LuaValue.True;
        else
            return LuaValue.False;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue NotEquals(LuaValue left, LuaValue right)
    {
        if (left.IsNil && right.IsNil)
            return LuaValue.False;
        else if (left.IsBoolean && right.IsBoolean && left.AsBoolean() == right.AsBoolean())
            return LuaValue.False;
        else if (left.IsNumber && right.IsNumber && left.ToNumber() == right.ToNumber())
            return LuaValue.False;
        else if (left.IsString && right.IsString && left.AsString() == right.AsString())
            return LuaValue.False;
        else
            return LuaValue.True;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue LessThan(LuaValue left, LuaValue right)
    {
        if (left.IsString && right.IsString)
            return new LuaValue(string.CompareOrdinal(left.AsString(), right.AsString()) < 0);
        else if (left.IsNumber && right.IsNumber)
            return new LuaValue(left.ToNumber() < right.ToNumber());
        else if (left.Kind == right.Kind)
            LuaException.ThrowCompare(KindToString(left.Kind));
        else
            LuaException.ThrowCompare(KindToString(left.Kind), KindToString(right.Kind));
        throw new UnreachableException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue LessThanOrEqual(LuaValue left, LuaValue right)
    {
        if (left.IsString && right.IsString)
            return new LuaValue(string.CompareOrdinal(left.AsString(), right.AsString()) <= 0);
        else if (left.IsNumber && right.IsNumber)
            return new LuaValue(left.ToNumber() <= right.ToNumber());
        else if (left.Kind == right.Kind)
            LuaException.ThrowCompare(KindToString(left.Kind));
        else
            LuaException.ThrowCompare(KindToString(left.Kind), KindToString(right.Kind));
        throw new UnreachableException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue GreaterThan(LuaValue left, LuaValue right)
    {
        if (left.IsString && right.IsString)
            return new LuaValue(string.CompareOrdinal(left.AsString(), right.AsString()) > 0);
        else if (left.IsNumber && right.IsNumber)
            return new LuaValue(left.ToNumber() > right.ToNumber());
        else if (left.Kind == right.Kind)
            LuaException.ThrowCompare(KindToString(left.Kind));
        else
            LuaException.ThrowCompare(KindToString(left.Kind), KindToString(right.Kind));
        throw new UnreachableException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue GreaterThanOrEqual(LuaValue left, LuaValue right)
    {
        if (left.IsString && right.IsString)
            return new LuaValue(string.CompareOrdinal(left.AsString(), right.AsString()) >= 0);
        else if (left.IsNumber && right.IsNumber)
            return new LuaValue(left.ToNumber() >= right.ToNumber());
        else if (left.Kind == right.Kind)
            LuaException.ThrowCompare(KindToString(left.Kind));
        else
            LuaException.ThrowCompare(KindToString(left.Kind), KindToString(right.Kind));
        throw new UnreachableException();
    }
}