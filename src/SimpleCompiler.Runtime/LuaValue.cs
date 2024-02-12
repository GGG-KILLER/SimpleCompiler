using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SimpleCompiler.Runtime;

public readonly struct LuaValue : IEquatable<LuaValue>
{
    public static readonly LuaValue Nil = new(ValueKind.Nil, null, null, default);
    public static readonly LuaValue True = new(true);
    public static readonly LuaValue False = new(false);

    public readonly ValueKind Kind;
    private readonly string? _strValue;
    private readonly LuaFunction? _luaFunction;
    private readonly ValueUnion _valueUnion;

    private LuaValue(ValueKind kind, string? strValue, LuaFunction? luaFunction, ValueUnion valueUnion)
    {
        Kind = kind;
        _strValue = strValue;
        _luaFunction = luaFunction;
        _valueUnion = valueUnion;
    }

    public LuaValue() : this(ValueKind.Nil, null, null, default)
    {
    }

    public LuaValue(string value) : this(ValueKind.String, value, null, default)
    {
        ArgumentNullException.ThrowIfNull(value);
    }

    public LuaValue(bool value) : this(ValueKind.Boolean, null, null, new ValueUnion { Boolean = value })
    {
    }

    public LuaValue(long value) : this(ValueKind.Long, null, null, new ValueUnion { Long = value })
    {
    }

    public LuaValue(double value) : this(ValueKind.Double, null, null, new ValueUnion { Double = value })
    {
    }

    public LuaValue(LuaFunction function) : this(ValueKind.Function, null, function, default)
    {
    }

    public bool IsNil => Kind == ValueKind.Nil;
    public bool IsString => Kind == ValueKind.String;
    public bool IsBoolean => Kind == ValueKind.Boolean;
    public bool IsLong => Kind == ValueKind.Long;
    public bool IsDouble => Kind == ValueKind.Double;
    public bool IsNumber => IsLong || IsDouble;
    public bool IsTruthy => !(IsNil || (IsBoolean && !_valueUnion.Boolean));
    public bool IsFunction => Kind == ValueKind.Function;

    public bool AsBoolean()
    {
        if (!IsBoolean)
            throw new LuaException($"Cannot cast a value of type {Kind} to Boolean.");
        return _valueUnion.Boolean;
    }

    public long AsLong()
    {
        if (!IsLong)
            throw new LuaException($"Cannot cast a value of type {Kind} to Long.");
        return _valueUnion.Long;
    }

    public double AsDouble()
    {
        if (!IsDouble)
            throw new LuaException($"Cannot cast a value of type {Kind} to Double.");
        return _valueUnion.Double;
    }

    public long ToInteger()
    {
        return Kind switch
        {
            ValueKind.Long => _valueUnion.Long,
            ValueKind.Double => LuaOperations.ToInt(_valueUnion.Double),
            ValueKind.String => long.TryParse(_strValue,
                                                NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent,
                                                CultureInfo.InvariantCulture, out var num)
                                ? num
                                : throw new LuaException($"String '{_strValue}' is not a valid integer."),
            _ => throw new LuaException($"Cannot cast a value of type {Kind} to an integer.")
        };
    }

    public double ToNumber()
    {
        return Kind switch
        {
            ValueKind.Long => _valueUnion.Long,
            ValueKind.Double => _valueUnion.Double,
            ValueKind.String => double.TryParse(_strValue,
                                                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                                                CultureInfo.InvariantCulture, out var num)
                                ? num
                                : throw new LuaException($"String '{_strValue}' is not a valid number."),
            _ => throw new LuaException($"Cannot cast a value of type {Kind} to a number.")
        };
    }

    public string AsString()
    {
        if (!IsString)
            throw new LuaException($"Cannot cast a value of type {Kind} to String.");
        return _strValue!;
    }

    public LuaFunction AsFunction()
    {
        if (!IsFunction)
            throw new LuaException($"Cannot cast a value of type {Kind} to function.");
        return _luaFunction!;
    }

    public override string ToString()
    {
        return Kind switch
        {
            ValueKind.Nil => "nil",
            ValueKind.Boolean => _valueUnion.Boolean ? "true" : "false",
            ValueKind.Long => _valueUnion.Long.ToString(),
            ValueKind.Double => _valueUnion.Double.ToString(),
            ValueKind.String => _strValue!,
            ValueKind.Function => $"function: 0x{_luaFunction!.GetHashCode():X}",
            _ => throw new UnreachableException(),
        };
    }

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is LuaValue value && Equals(value);

    public bool Equals(LuaValue other)
    {
        return Kind == other.Kind
            && Kind switch
            {
                ValueKind.Nil => true,
                ValueKind.Boolean => _valueUnion.Boolean == other._valueUnion.Boolean,
                ValueKind.Long => _valueUnion.Long == other._valueUnion.Long,
                ValueKind.Double => _valueUnion.Double == other._valueUnion.Double,
                ValueKind.String => _strValue == other._strValue,
                ValueKind.Function => _luaFunction == other._luaFunction,
                _ => false
            };
    }

    public override int GetHashCode()
    {
        return Kind switch
        {
            ValueKind.Nil => 0,
            ValueKind.Boolean => _valueUnion.Boolean.GetHashCode(),
            ValueKind.Long => _valueUnion.Long.GetHashCode(),
            ValueKind.Double => _valueUnion.Double.GetHashCode(),
            ValueKind.String => _strValue!.GetHashCode(),
            ValueKind.Function => _luaFunction!.GetHashCode(),
            _ => -1
        };
    }

    public static bool operator ==(LuaValue left, LuaValue right) => left.Equals(right);

    public static bool operator !=(LuaValue left, LuaValue right) => !(left == right);

    [StructLayout(LayoutKind.Explicit)]
    private struct ValueUnion
    {
        [FieldOffset(0)]
        public bool Boolean;
        [FieldOffset(0)]
        public long Long;
        [FieldOffset(0)]
        public double Double;
    }
}
