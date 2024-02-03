using Tsu.Trees.RedGreen;

namespace SimpleCompiler.MIR.Internal;

[GreenNode(MirKind.FunctionCallExpression)]
internal sealed partial class FunctionCallExpression : Expression
{
    private readonly Expression _callee;
    [GreenList(typeof(Expression))]
    private readonly MirNode? _arguments;
}
