using Tsu.Trees.RedGreen;

namespace SimpleCompiler.MIR.Internal;

[GreenNode(MirKind.StatementList)]
internal sealed partial class StatementList : Statement
{
    [GreenList(typeof(Statement))]
    private readonly MirNode? _statements;
    private readonly ScopeInfo? _scopeInfo;
}
