using Loretta.CodeAnalysis;
using SimpleCompiler.Emit;
using SimpleCompiler.IR;
using SimpleCompiler.IR.Optimizations;

namespace SimpleCompiler.Compiler;

public sealed class Compilation(SyntaxTree syntaxTree)
{
    private readonly SyntaxTree _syntaxTree = syntaxTree;
    private IrTree? _mirRoot;
    private IrTree? _optimizedIrRoot;

    public IrTree LowerSyntax()
    {
        if (_mirRoot is null)
        {
            Interlocked.CompareExchange(ref _mirRoot, IrTree.FromSyntax(_syntaxTree)!, null);
        }
        return _mirRoot;
    }

    public IrTree OptimizeLoweredSyntax(Action<IrNode, string>? onOptimizationRan = null)
    {
        if (_optimizedIrRoot is null)
        {
            var tree = LowerSyntax();
            var root = tree.Root;

            root = new Inliner(tree).Visit(root);
            onOptimizationRan?.Invoke(root, "Inlining");

            root = ConstantFolder.ConstantFold(root);
            onOptimizationRan?.Invoke(root, "Constant Folding");

            var globalScope = new ScopeInfo(ScopeKind.Global, null);
            root = new ScopeRemapper(globalScope).Visit(root)!;
            onOptimizationRan?.Invoke(root, "Scope Remapping");

            Interlocked.CompareExchange(ref _optimizedIrRoot, IrTree.FromRoot(globalScope, root), null);
        }
        return _optimizedIrRoot;
    }

    public async Task EmitAsync(string name, Stream stream, TextWriter? cilDebugWriter = null) =>
        await Emitter.EmitAsync(name, OptimizeLoweredSyntax(), stream, cilDebugWriter)
                     .ConfigureAwait(false);
}
