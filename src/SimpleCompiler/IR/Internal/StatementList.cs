using Tsu.Trees.RedGreen;

namespace SimpleCompiler.IR.Internal;

[GreenNode(IrKind.StatementList)]
internal sealed partial class StatementList : Statement
{
    [GreenList(typeof(Statement))]
    private readonly IrNode? _statements;
    private readonly ScopeInfo? _scopeInfo;
}
