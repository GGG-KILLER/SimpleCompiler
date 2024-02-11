using Loretta.CodeAnalysis;
using SimpleCompiler.IR.Ssa;

namespace SimpleCompiler.IR;

public sealed class IrTree
{
    public IrTree(ScopeInfo globalScope, IrNode root)
    {
        GlobalScope = globalScope;
        Ssa = new SsaComputer(this);
        Root = root;
    }

    public ScopeInfo GlobalScope { get; }
    public SsaComputer Ssa { get; }
    public IrNode Root { get; }
}
