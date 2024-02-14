using SimpleCompiler.Backends;
using SimpleCompiler.FileSystem;
using SimpleCompiler.Frontends;
using SimpleCompiler.IR;

namespace SimpleCompiler.Compiler;

public sealed class Compilation<TInput>(IFrontend<TInput> frontend, IBackend backend)
{
    public IrGraph GetIrGraph(TInput input) => frontend.Lower(input);

    public IrGraph GetOptimizedIrGraph(TInput input, Action<IrGraph, string>? onOptimizationRan = null)
    {
        var ir = frontend.Lower(input);
        // TODO: Optimization steps
        return ir;
    }

    public async Task EmitAsync(TInput input, string name, IOutputManager output, bool optimize = true, CancellationToken cancellationToken = default) =>
        await backend.EmitToDirectory(new EmitOptions(name), optimize ? GetOptimizedIrGraph(input) : GetIrGraph(input), output, cancellationToken)
                     .ConfigureAwait(false);
}
