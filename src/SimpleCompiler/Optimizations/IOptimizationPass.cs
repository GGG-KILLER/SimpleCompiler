using SimpleCompiler.IR;

namespace SimpleCompiler.Optimizations;

public interface IOptimizationPass
{
    void Execute(IrGraph graph);
}
