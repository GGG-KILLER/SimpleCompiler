namespace SimpleCompiler.IR;

public sealed class BasicBlock(int blockOrdinal, IEnumerable<Instruction> instructions)
{
    /// <summary>
    /// This block's index in the <see cref="IrGraph.BasicBlocks"/>.
    /// </summary>
    public int Ordinal { get; } = blockOrdinal;

    /// <summary>
    /// This block's instructions.
    /// </summary>
    public List<Instruction> Instructions { get; } = instructions.ToList();
}
