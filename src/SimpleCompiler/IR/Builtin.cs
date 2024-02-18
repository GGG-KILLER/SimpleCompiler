namespace SimpleCompiler.IR;

public sealed class Builtin(KnownBuiltins builtinId) : Operand, IEquatable<Builtin>
{
    public KnownBuiltins BuiltinId { get; } = builtinId;

    public override bool Equals(object? obj) => Equals(obj as Builtin);
    public bool Equals(Builtin? other) =>
        other is not null && BuiltinId == other.BuiltinId;

    public override int GetHashCode() => HashCode.Combine(BuiltinId);

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
