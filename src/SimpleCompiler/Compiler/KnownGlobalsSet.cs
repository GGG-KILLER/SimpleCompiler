using SimpleCompiler.MIR;

namespace SimpleCompiler.Compiler;

public sealed class KnownGlobalsSet
{
    internal KnownGlobalsSet(ScopeInfo globalScope)
    {
        Print = new VariableInfo(globalScope, VariableKind.Global, "print");
        ToString = new VariableInfo(globalScope, VariableKind.Global, "tostring");
    }

    public VariableInfo Print { get; }
    public new VariableInfo ToString { get; }
}
