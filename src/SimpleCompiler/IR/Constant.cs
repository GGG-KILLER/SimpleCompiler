namespace SimpleCompiler.IR;

public sealed class Constant(ConstantKind kind, object? value) : Operand
{
    public static readonly Constant Nil = new(ConstantKind.Nil, null);
    public static readonly Constant True = new(ConstantKind.Boolean, true);
    public static readonly Constant False = new(ConstantKind.Boolean, false);

    public ConstantKind Kind { get; } = kind;
    public object? Value { get; } = value;

    public override string ToString() => Kind == ConstantKind.Nil ? "nil" : Value!.ToString()!;
}
