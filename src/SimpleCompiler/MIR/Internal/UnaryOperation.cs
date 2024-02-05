using Tsu.Trees.RedGreen;

namespace SimpleCompiler.MIR.Internal;

[GreenNode(MirKind.UnaryOperationExpression)]
internal sealed partial class UnaryOperationExpression : Expression
{
    private readonly UnaryOperationKind _unaryOperationKind;
    private readonly Expression _operand;
}
