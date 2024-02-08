using Tsu.Trees.RedGreen;

namespace SimpleCompiler.MIR.Internal;

[GreenNode(MirKind.ConstantExpression)]
internal sealed partial class ConstantExpression: Expression
{
    private readonly ConstantKind _constantKind;
    private readonly object? _value;
}
