using Tsu.Trees.RedGreen;

namespace SimpleCompiler.IR.Internal;

[GreenNode(IrKind.FunctionCallExpression)]
internal sealed partial class FunctionCallExpression : Expression
{
    private readonly Expression _callee;
    [GreenList(typeof(Expression))]
    private readonly IrNode? _arguments;
}
