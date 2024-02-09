using Tsu.Trees.RedGreen;

namespace SimpleCompiler.IR.Internal;

[GreenNode(IrKind.ExpressionStatement)]
internal sealed partial class ExpressionStatement : Statement
{
    private readonly Expression _expression;
}
