using SimpleCompiler.Backends;
using SimpleCompiler.Optimizations;
using SimpleCompiler.FileSystem;
using SimpleCompiler.Frontends;
using SimpleCompiler.IR;
using System.Collections.Immutable;

namespace SimpleCompiler;

public sealed class Compilation<TInput>(
    IFrontend<TInput> frontend,
    IEnumerable<IOptimizationPass> optimizationPasses,
    IBackend backend)
{
    private readonly ImmutableArray<IOptimizationPass> _optimizationPasses = optimizationPasses.ToImmutableArray();

    public IrGraph GetIrGraph(TInput input) => frontend.Lower(input);

    public IrGraph GetOptimizedIrGraph(TInput input, Action<IrGraph, string>? onOptimizationRan = null)
    {
        var ir = frontend.Lower(input);
        foreach (var pass in _optimizationPasses)
        {
            pass.Execute(ir);
            onOptimizationRan?.Invoke(ir, pass.GetType().Name);
        }
        return ir;
    }

    public async Task EmitAsync(TInput input, string name, IOutputManager output, bool optimize = true, CancellationToken cancellationToken = default) =>
        await backend.EmitToDirectory(new EmitOptions(name), optimize ? GetOptimizedIrGraph(input) : GetIrGraph(input), output, cancellationToken)
                     .ConfigureAwait(false);
}
