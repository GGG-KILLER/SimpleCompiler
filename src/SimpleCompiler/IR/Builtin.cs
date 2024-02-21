using System.Diagnostics;

namespace SimpleCompiler.IR;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public sealed class Builtin(KnownBuiltins builtinId) : Operand, IEquatable<Builtin>
{
    public KnownBuiltins BuiltinId { get; } = builtinId;

    public override bool Equals(object? obj) => Equals(obj as Builtin);
    public override bool Equals(Operand? other) => Equals(other as Builtin);
    public bool Equals(Builtin? other) =>
        other is not null && BuiltinId == other.BuiltinId;
    public override int GetHashCode() => HashCode.Combine(BuiltinId);

    public override string ToString() => $"${BuiltinId}";
    private string GetDebuggerDisplay() => ToString();

    public static bool operator ==(Builtin left, Builtin right) => left.Equals(right);
    public static bool operator !=(Builtin left, Builtin right) => !(left == right);
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
