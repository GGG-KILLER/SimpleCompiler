using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SimpleCompiler.Runtime;

public static partial class LuaOperations
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(LuaValue left, LuaValue right) =>
        left.IsNil && right.IsNil
        || left.IsBoolean && right.IsBoolean && left.AsBoolean() == right.AsBoolean()
        || left.IsNumber && right.IsNumber && left.ToNumber() == right.ToNumber()
        || left.IsString && right.IsString && left.AsString() == right.AsString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NotEquals(LuaValue left, LuaValue right) => !Equals(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThan(LuaValue left, LuaValue right)
    {
        if (left.IsString && right.IsString)
            return string.CompareOrdinal(left.AsString(), right.AsString()) < 0;
        else if (left.IsNumber && right.IsNumber)
            return left.ToNumber() < right.ToNumber();
        else if (left.Kind == right.Kind)
            LuaException.ThrowCompare(KindToString(left.Kind));
        else
            LuaException.ThrowCompare(KindToString(left.Kind), KindToString(right.Kind));
        throw new UnreachableException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanOrEqual(LuaValue left, LuaValue right)
    {
        if (left.IsString && right.IsString)
            return string.CompareOrdinal(left.AsString(), right.AsString()) <= 0;
        else if (left.IsNumber && right.IsNumber)
            return left.ToNumber() <= right.ToNumber();
        else if (left.Kind == right.Kind)
            LuaException.ThrowCompare(KindToString(left.Kind));
        else
            LuaException.ThrowCompare(KindToString(left.Kind), KindToString(right.Kind));
        throw new UnreachableException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThan(LuaValue left, LuaValue right)
    {
        if (left.IsString && right.IsString)
            return string.CompareOrdinal(left.AsString(), right.AsString()) > 0;
        else if (left.IsNumber && right.IsNumber)
            return left.ToNumber() > right.ToNumber();
        else if (left.Kind == right.Kind)
            LuaException.ThrowCompare(KindToString(left.Kind));
        else
            LuaException.ThrowCompare(KindToString(left.Kind), KindToString(right.Kind));
        throw new UnreachableException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanOrEqual(LuaValue left, LuaValue right)
    {
        if (left.IsString && right.IsString)
            return string.CompareOrdinal(left.AsString(), right.AsString()) >= 0;
        else if (left.IsNumber && right.IsNumber)
            return left.ToNumber() >= right.ToNumber();
        else if (left.Kind == right.Kind)
            LuaException.ThrowCompare(KindToString(left.Kind));
        else
            LuaException.ThrowCompare(KindToString(left.Kind), KindToString(right.Kind));
        throw new UnreachableException();
    }
}
