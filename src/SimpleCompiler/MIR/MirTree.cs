using Loretta.CodeAnalysis;

namespace SimpleCompiler.MIR;

public sealed class MirTree
{
    private MirTree(ScopeInfo globalScope, MirNode root)
    {
        GlobalScope = globalScope;
        Root = root;
    }

    public ScopeInfo GlobalScope { get; }

    public MirNode Root { get; }

    public static MirTree FromSyntax(SyntaxTree syntaxTree)
    {
        var globalScope = new ScopeInfo(ScopeKind.Global, null);
        var root = new SyntaxLowerer(globalScope).Visit(syntaxTree.GetRoot())!;

        return FromRoot(globalScope, root);
    }

    public static MirTree FromRoot(ScopeInfo globalScope, MirNode root) => new(globalScope, root);
}
