namespace SimpleCompiler.IR;

public sealed class Builtin(KnownBuiltins builtinId) : Operand
{
    public KnownBuiltins BuiltinId { get; } = builtinId;

    public override string ToString() => $"${BuiltinId}";
}

public enum KnownBuiltins
{
    INVALID = 0,

    assert,
    type,
    print,
    error,
    tostring
}
