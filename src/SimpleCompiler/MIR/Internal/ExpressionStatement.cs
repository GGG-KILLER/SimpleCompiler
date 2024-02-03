using Tsu.Trees.RedGreen;

namespace SimpleCompiler.MIR.Internal;

[GreenNode(MirKind.ExpressionStatement)]
internal sealed partial class ExpressionStatement : Statement
{
    private readonly Expression _expression;
}
