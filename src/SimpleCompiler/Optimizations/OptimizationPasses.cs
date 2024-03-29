namespace SimpleCompiler.Optimizations;

public static class OptimizationPasses
{
    public static IEnumerable<IOptimizationPass> All => [
        new ConstantFoldingAndPropagation(),
        new DeadCodeElimination(),
        new ConstantFoldingAndPropagation()
    ];
}
