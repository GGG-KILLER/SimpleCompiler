namespace SimpleCompiler.IR;

public sealed class IrGraph(
    List<BasicBlock> basicBlocks,
    List<IrEdge> edges,
    BasicBlock entryBlock)
{
    public List<BasicBlock> BasicBlocks { get; } = basicBlocks;
    public List<IrEdge> Edges { get; } = edges;
    public BasicBlock EntryBlock { get; set; } = entryBlock;

    public IEnumerable<BasicBlock> GetPredecessors(int blockOrdinal) =>
        Edges.Where(x => x.TargetBlockOrdinal == blockOrdinal).Select(x => BasicBlocks[x.SourceBlockOrdinal]);

    public IEnumerable<BasicBlock> GetSucessors(int blockOrdinal) =>
        Edges.Where(x => x.SourceBlockOrdinal == blockOrdinal).Select(x => BasicBlocks[x.TargetBlockOrdinal]);

    public BasicBlock FindBlock(Instruction instruction) =>
        BasicBlocks.Single(x => x.Instructions.Contains(instruction));

    public Instruction FindDefinition(NameValue name) =>
        BasicBlocks.SelectMany(x => x.Instructions).Single(x => x.IsAssignment && x.Assignee == name);

    public IEnumerable<Instruction> FindUses(NameValue name) =>
        BasicBlocks.SelectMany(x => x.Instructions).Where(x => x.References(name));

    public IrGraph Clone()
    {
        var basicBlocks = BasicBlocks.Select(x => x.Clone()).ToList();
        var edges = Edges.ToList();
        var entryBlock = basicBlocks[EntryBlock.Ordinal];
        return new(basicBlocks, edges, entryBlock);
    }
}

public readonly record struct IrEdge(int SourceBlockOrdinal, int TargetBlockOrdinal);
