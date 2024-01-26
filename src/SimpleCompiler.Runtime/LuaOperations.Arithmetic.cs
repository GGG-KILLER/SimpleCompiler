using System.Runtime.CompilerServices;

namespace SimpleCompiler.Runtime;

public static partial class LuaOperations
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue Neg(LuaValue operand)
    {
        if (!(operand.IsNumber || operand.IsString))
        {
            LuaException.ThrowArithmetic(KindToString(operand.Kind));
        }

        return new LuaValue(-operand.ToNumber());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue Add(LuaValue left, LuaValue right)
    {
        if ((!left.IsNumber && !left.IsString) || (!right.IsNumber && !right.IsString))
        {
            if (left.IsNil || right.IsNil)
                LuaException.ThrowArithmetic("nil");
            else if (left.IsBoolean || right.IsBoolean)
                LuaException.ThrowArithmetic("boolean");
        }

        if (left.IsLong && right.IsLong)
            return new LuaValue(left.AsLong() + right.AsLong());
        else
            return new LuaValue(left.ToNumber() + right.ToNumber());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue Subtract(LuaValue left, LuaValue right)
    {
        if ((!left.IsNumber && !left.IsString) || (!right.IsNumber && !right.IsString))
        {
            if (left.IsNil || right.IsNil)
                LuaException.ThrowArithmetic("nil");
            else if (left.IsBoolean || right.IsBoolean)
                LuaException.ThrowArithmetic("boolean");
        }

        if (left.IsLong && right.IsLong)
            return new LuaValue(left.AsLong() - right.AsLong());
        else
            return new LuaValue(left.ToNumber() - right.ToNumber());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue Multiply(LuaValue left, LuaValue right)
    {
        if ((!left.IsNumber && !left.IsString) || (!right.IsNumber && !right.IsString))
        {
            if (left.IsNil || right.IsNil)
                LuaException.ThrowArithmetic("nil");
            else if (left.IsBoolean || right.IsBoolean)
                LuaException.ThrowArithmetic("boolean");
        }

        if (left.IsLong && right.IsLong)
            return new LuaValue(left.AsLong() * right.AsLong());
        else
            return new LuaValue(left.ToNumber() * right.ToNumber());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue Divide(LuaValue left, LuaValue right)
    {
        if ((!left.IsNumber && !left.IsString) || (!right.IsNumber && !right.IsString))
        {
            if (left.IsNil || right.IsNil)
                LuaException.ThrowArithmetic("nil");
            else if (left.IsBoolean || right.IsBoolean)
                LuaException.ThrowArithmetic("boolean");
        }

        if (left.IsLong && right.IsLong)
            return new LuaValue(left.AsLong() / right.AsLong());
        else
            return new LuaValue(left.ToNumber() / right.ToNumber());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue IntegerDivide(LuaValue left, LuaValue right)
    {
        if ((!left.IsNumber && !left.IsString) || (!right.IsNumber && !right.IsString))
        {
            if (left.IsNil || right.IsNil)
                LuaException.ThrowArithmetic("nil");
            else if (left.IsBoolean || right.IsBoolean)
                LuaException.ThrowArithmetic("boolean");
        }

        return new LuaValue((long)Math.Floor(left.ToNumber() / right.ToNumber()));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue Exponentiate(LuaValue left, LuaValue right)
    {
        if ((!left.IsNumber && !left.IsString) || (!right.IsNumber && !right.IsString))
        {
            if (left.IsNil || right.IsNil)
                LuaException.ThrowArithmetic("nil");
            else if (left.IsBoolean || right.IsBoolean)
                LuaException.ThrowArithmetic("boolean");
        }

        return new LuaValue(Math.Pow(left.ToNumber(), right.ToNumber()));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuaValue Modulo(LuaValue left, LuaValue right)
    {
        if ((!left.IsNumber && !left.IsString) || (!right.IsNumber && !right.IsString))
        {
            if (left.IsNil || right.IsNil)
                LuaException.ThrowArithmetic("nil");
            else if (left.IsBoolean || right.IsBoolean)
                LuaException.ThrowArithmetic("boolean");
        }

        if (left.IsLong && right.IsLong)
            return new LuaValue(left.AsLong() % right.AsLong());
        else
            return new LuaValue(left.ToNumber() % right.ToNumber());
    }
}