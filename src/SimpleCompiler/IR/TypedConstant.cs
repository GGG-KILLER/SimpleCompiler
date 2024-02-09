namespace SimpleCompiler.IR;

public readonly struct TypedConstant
{
    public static TypedConstant None => new();
    public static TypedConstant Nil => new(null);
    public static TypedConstant True => new(true);
    public static TypedConstant False => new(false);

    private readonly bool _hasValue;
    private readonly object? _value;

    public TypedConstant()
    {
        _hasValue = false;
        _value = null;
    }

    public TypedConstant(object? value)
    {
        _hasValue = true;
        _value = value;
    }

    public bool HasValue => _hasValue;
    public object? Value => _hasValue ? _value : throw new InvalidOperationException("TypedConstant has no value.");
    public bool IsFalsey => _value is null or false;
}
