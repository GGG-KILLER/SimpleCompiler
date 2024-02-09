using Tsu.Trees.RedGreen;

namespace SimpleCompiler.IR.Internal;

[GreenNode(IrKind.ConstantExpression)]
internal sealed partial class ConstantExpression: Expression
{
    private readonly ConstantKind _constantKind;
    private readonly object? _value;
}
