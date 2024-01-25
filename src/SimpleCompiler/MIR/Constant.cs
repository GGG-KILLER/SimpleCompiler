namespace SimpleCompiler.MIR;

public enum ConstantKind
{
    Nil,
    Boolean,
    Number,
    String
}

[Tsu.TreeSourceGen.TreeNode(typeof(MirNode))]
public sealed partial class Constant(ConstantKind constantKind, object value) : Expression
{
    public static Constant Nil => new(ConstantKind.Nil, null!);
    public static Constant True => new(ConstantKind.Boolean, true);
    public static Constant False => new(ConstantKind.Boolean, false);

    public ConstantKind ConstantKind { get; } = constantKind;
    public object Value { get; } = value;

    public override IEnumerable<MirNode> GetChildren() => [];
}
