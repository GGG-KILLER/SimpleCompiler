using System.Collections.Immutable;

namespace SimpleCompiler.MIR;

[Tsu.TreeSourceGen.TreeNode(typeof(MirNode))]
public sealed partial class StatementList : Statement
{
    public ImmutableArray<Statement> Statements { get; }
    public ScopeInfo? ScopeInfo { get; }

    public StatementList(IEnumerable<Statement> statements, ScopeInfo? scopeInfo)
    {
        Statements = statements.ToImmutableArray();
        foreach (var stmt in Statements) stmt.Parent = this;
        ScopeInfo = scopeInfo;
    }

    public override IEnumerable<MirNode> GetChildren() => Statements;
}
