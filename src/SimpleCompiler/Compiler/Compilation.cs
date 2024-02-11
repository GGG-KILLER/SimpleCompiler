using SimpleCompiler.Backends;
using SimpleCompiler.FileSystem;
using SimpleCompiler.Frontends;
using SimpleCompiler.IR;
using SimpleCompiler.IR.Optimizations;

namespace SimpleCompiler.Compiler;

public sealed class Compilation(IFrontend frontend, IBackend backend)
{
    private IrTree? _mirRoot;
    private IrTree? _optimizedIrRoot;

    public IrTree GetTree()
    {
        if (_mirRoot is null)
        {
            Interlocked.CompareExchange(ref _mirRoot, frontend.GetTree(), null);
        }
        return _mirRoot;
    }

    public IrTree GetOptimizedTree(Action<IrNode, string>? onOptimizationRan = null)
    {
        if (_optimizedIrRoot is null)
        {
            var tree = GetTree();
            var root = tree.Root;

            root = new Inliner(tree).Visit(root);
            onOptimizationRan?.Invoke(root, "Inlining");

            root = ConstantFolder.ConstantFold(root);
            onOptimizationRan?.Invoke(root, "Constant Folding");

            var globalScope = new ScopeInfo(ScopeKind.Global, null);
            root = new ScopeRemapper(globalScope).Visit(root)!;
            onOptimizationRan?.Invoke(root, "Scope Remapping");

            Interlocked.CompareExchange(ref _optimizedIrRoot, new IrTree(globalScope, root), null);
        }
        return _optimizedIrRoot;
    }

    public async Task EmitAsync(string name, IOutputManager output, bool optimize = true, CancellationToken cancellationToken = default) =>
        await backend.EmitToDirectory(new EmitOptions(name), optimize ? GetOptimizedTree() : GetTree(), output, cancellationToken)
                     .ConfigureAwait(false);
}
