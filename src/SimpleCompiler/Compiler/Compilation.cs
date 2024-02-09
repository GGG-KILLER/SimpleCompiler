using Loretta.CodeAnalysis;
using SimpleCompiler.Emit;
using SimpleCompiler.LIR;
using SimpleCompiler.MIR;
using SimpleCompiler.MIR.Optimizations;

namespace SimpleCompiler.Compiler;

public sealed class Compilation(SyntaxTree syntaxTree)
{
    private readonly SyntaxTree _syntaxTree = syntaxTree;
    private MirTree? _mirRoot;
    private MirTree? _optimizedMirRoot;
    private IReadOnlyList<Instruction>? _lir;

    public MirTree LowerSyntax()
    {
        if (_mirRoot is null)
        {
            Interlocked.CompareExchange(ref _mirRoot, MirTree.FromSyntax(_syntaxTree)!, null);
        }
        return _mirRoot;
    }

    public MirTree OptimizeLoweredSyntax(Action<MirNode, string>? onOptimizationRan = null)
    {
        if (_optimizedMirRoot is null)
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

            Interlocked.CompareExchange(ref _optimizedMirRoot, MirTree.FromRoot(globalScope, root), null);
        }
        return _optimizedMirRoot;
    }

    public IEnumerable<Instruction> LowerMir()
    {
        if (_lir is null)
        {
            var node = OptimizeLoweredSyntax().Root;
            Interlocked.CompareExchange(ref _lir, MirLowerer.Lower(node), null);
        }
        return _lir;
    }

    public async Task EmitAsync(string name, Stream stream, TextWriter? cilDebugWriter = null) =>
        await Emitter.EmitAsync(name, OptimizeLoweredSyntax().GlobalScope.KnownGlobals, stream, LowerMir(), cilDebugWriter)
                     .ConfigureAwait(false);
}
