using Tsu.Trees.RedGreen;

namespace SimpleCompiler.IR.Internal;

[GreenNode(IrKind.BinaryOperationExpression)]
internal sealed partial class BinaryOperationExpression : Expression
{
    private readonly BinaryOperationKind _binaryOperationKind;
    private readonly Expression _left;
    private readonly Expression _right;
}