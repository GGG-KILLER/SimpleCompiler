using Loretta.CodeAnalysis;
using SimpleCompiler.MIR.Ssa;

namespace SimpleCompiler.MIR;

public sealed class MirTree
{
    private MirTree(ScopeInfo globalScope, MirNode root)
    {
        GlobalScope = globalScope;
        Ssa = new SsaComputer(this);
        Root = root;
    }

    public ScopeInfo GlobalScope { get; }
    public SsaComputer Ssa { get; }
    public MirNode Root { get; }

    public static MirTree FromSyntax(SyntaxTree syntaxTree)
    {
        var globalScope = new ScopeInfo(ScopeKind.Global, null);
        var root = new SyntaxLowerer(globalScope).Visit(syntaxTree.GetRoot())!;

        return FromRoot(globalScope, root);
    }

    public static MirTree FromRoot(ScopeInfo globalScope, MirNode root) => new(globalScope, root);
}
