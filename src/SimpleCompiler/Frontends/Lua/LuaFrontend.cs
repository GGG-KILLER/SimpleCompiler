using Loretta.CodeAnalysis;
using SimpleCompiler.IR;

namespace SimpleCompiler.Frontends.Lua;

public sealed class LuaFrontend(SyntaxTree syntaxTree) : IFrontend
{
    public IrTree GetTree()
    {
        var globalScope = new ScopeInfo(ScopeKind.Global, null);
        return new IrTree(globalScope, new SyntaxLowerer(globalScope).Visit(syntaxTree.GetRoot())!);
    }
}
