namespace SimpleCompiler.MIR;

[Tsu.TreeSourceGen.TreeNode(typeof(MirNode))]
public sealed partial class BinaryOperation : Expression
{
    public BinaryOperationKind BinaryOperationKind { get; }
    public Expression Left { get; }
    public Expression Right { get; }

    public BinaryOperation(BinaryOperationKind binaryOperationKind, Expression left, Expression right)
    {
        BinaryOperationKind = binaryOperationKind;
        Left = left;
        Left.Parent = this;
        Right = right;
        Right.Parent = this;
    }

    public override IEnumerable<MirNode> GetChildren()
    {
        yield return Left;
        yield return Right;
    }
}
