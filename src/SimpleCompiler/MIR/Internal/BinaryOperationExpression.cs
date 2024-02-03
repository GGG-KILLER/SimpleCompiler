using Tsu.Trees.RedGreen;

namespace SimpleCompiler.MIR.Internal;

[GreenNode(MirKind.BinaryOperationExpression)]
internal sealed partial class BinaryOperationExpression : Expression
{
    [NodeComponent(Order = 0)]
    private readonly BinaryOperationKind _binaryOperationKind;
    private readonly Expression _left;
    private readonly Expression _right;
}
