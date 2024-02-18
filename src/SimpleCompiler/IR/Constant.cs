namespace SimpleCompiler.IR;

public sealed class Constant(ConstantKind kind, object? value) : Operand, IEquatable<Constant>
{
    public static readonly Constant Nil = new(ConstantKind.Nil, null);
    public static readonly Constant True = new(ConstantKind.Boolean, true);
    public static readonly Constant False = new(ConstantKind.Boolean, false);

    public ConstantKind Kind { get; } = kind;
    public object? Value { get; } = value;

    public override bool Equals(object? obj) => Equals(obj as Constant);
    public bool Equals(Constant? other) =>
        other is not null
        && Kind == other.Kind
        && Value == other.Value;

    public override int GetHashCode() => HashCode.Combine(Kind, Value);

    public override string ToString() => Kind == ConstantKind.Nil ? "nil" : Value!.ToString()!;
}
