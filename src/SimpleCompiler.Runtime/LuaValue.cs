using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SimpleCompiler.Runtime;

public readonly struct LuaValue
{
    public static readonly LuaValue Nil = new(ValueKind.Nil, null, default);
    public static readonly LuaValue True = new(true);
    public static readonly LuaValue False = new(false);

    public readonly ValueKind Kind;
    private readonly string? _strValue;
    private readonly ValueUnion _valueUnion;

    private LuaValue(ValueKind kind, string? strValue, ValueUnion valueUnion)
    {
        Kind = kind;
        _strValue = strValue;
        _valueUnion = valueUnion;
    }

    public LuaValue(string value) : this(ValueKind.String, value, default)
    {
        ArgumentNullException.ThrowIfNull(value);
    }

    public LuaValue(bool value) : this(ValueKind.Boolean, null, new ValueUnion { Boolean = value })
    {
    }

    public LuaValue(long value) : this(ValueKind.Boolean, null, new ValueUnion { Long = value })
    {
    }

    public LuaValue(double value) : this(ValueKind.Boolean, null, new ValueUnion { Double = value })
    {
    }

    public bool IsNil => Kind == ValueKind.Nil;
    public bool IsString => Kind == ValueKind.String;
    public bool IsBoolean => Kind == ValueKind.Boolean;
    public bool IsLong => Kind == ValueKind.Long;
    public bool IsDouble => Kind == ValueKind.Double;
    public bool IsNumber => IsLong || IsDouble;
    public bool IsTruthy => !(IsNil || (IsBoolean && !_valueUnion.Boolean));

    public bool AsBoolean()
    {
        if (!IsBoolean)
            throw new InvalidCastException($"Cannot cast a value of type {Kind} to Boolean.");
        return _valueUnion.Boolean;
    }

    public long AsLong()
    {
        if (!IsLong)
            throw new InvalidCastException($"Cannot cast a value of type {Kind} to Long.");
        return _valueUnion.Long;
    }

    public double AsDouble()
    {
        if (!IsDouble)
            throw new InvalidCastException($"Cannot cast a value of type {Kind} to Double.");
        return _valueUnion.Double;
    }

    public long ToInteger()
    {
        return Kind switch
        {
            ValueKind.Long => _valueUnion.Long,
            ValueKind.Double => Math.Truncate(_valueUnion.Double) == _valueUnion.Double
                                ? (int)Math.Truncate(_valueUnion.Double)
                                : throw new LuaException($"Number {_valueUnion.Double} is not a valid integer value."),
            ValueKind.String => long.TryParse(_strValue,
                                                NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent,
                                                CultureInfo.InvariantCulture, out var num)
                                ? num
                                : throw new InvalidCastException($"String '{_strValue}' is not a valid integer."),
            _ => throw new InvalidCastException($"Cannot cast a value of type {Kind} to an integer.")
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
                                : throw new InvalidCastException($"String '{_strValue}' is not a valid number."),
            _ => throw new InvalidCastException($"Cannot cast a value of type {Kind} to a number.")
        };
    }

    public string AsString()
    {
        if (!IsString)
            throw new InvalidCastException($"Cannot cast a value of type {Kind} to String.");
        return _strValue!;
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
            _ => throw new UnreachableException(),
        };
    }

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
