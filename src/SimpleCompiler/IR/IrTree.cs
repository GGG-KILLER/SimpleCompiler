using Loretta.CodeAnalysis;
using SimpleCompiler.IR.Ssa;

namespace SimpleCompiler.IR;

public sealed class IrTree
{
    private IrTree(ScopeInfo globalScope, IrNode root)
    {
        GlobalScope = globalScope;
        Ssa = new SsaComputer(this);
        Root = root;
    }

    public ScopeInfo GlobalScope { get; }
    public SsaComputer Ssa { get; }
    public IrNode Root { get; }

    public static IrTree FromSyntax(SyntaxTree syntaxTree)
    {
        var globalScope = new ScopeInfo(ScopeKind.Global, null);
        var root = new SyntaxLowerer(globalScope).Visit(syntaxTree.GetRoot())!;

        return FromRoot(globalScope, root);
    }

    public static IrTree FromRoot(ScopeInfo globalScope, IrNode root) => new(globalScope, root);
}
