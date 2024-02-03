using Tsu.Trees.RedGreen;

namespace SimpleCompiler.MIR.Internal;

[GreenNode(MirKind.UnaryOperationExpression)]
internal sealed partial class UnaryOperationExpression : Expression
{
    [NodeComponent(Order = 0)]
    private readonly UnaryOperationKind _unaryOperationKind;
    private readonly Expression _operand;
}
