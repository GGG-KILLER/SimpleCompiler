
namespace SimpleCompiler.MIR;

public enum UnaryOperationKind
{
    LogicalNegation,
    BitwiseNegation,
    NumericalNegation,
    LengthOf,
}

[Tsu.TreeSourceGen.TreeNode(typeof(MirNode))]
public sealed partial class UnaryOperation : Expression
{
    public UnaryOperationKind UnaryOperationKind { get; }
    public Expression Operand { get; }

    public UnaryOperation(UnaryOperationKind unaryOperationKind, Expression operand)
    {
        UnaryOperationKind = unaryOperationKind;
        Operand = operand;
        Operand.Parent = this;
    }

    public override IEnumerable<MirNode> GetChildren() => [Operand];
}
