using Tsu.Trees.RedGreen;

namespace SimpleCompiler.IR.Internal;

[GreenNode(IrKind.UnaryOperationExpression)]
internal sealed partial class UnaryOperationExpression : Expression
{
    private readonly UnaryOperationKind _unaryOperationKind;
    private readonly Expression _operand;
}
