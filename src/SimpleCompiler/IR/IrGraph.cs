using System.Collections.Immutable;

namespace SimpleCompiler.IR;

public sealed class IrGraph(
    ImmutableArray<BasicBlock> basicBlocks,
    ImmutableArray<IrEdge> edges,
    ImmutableArray<BasicBlock> entryBlocks)
{
    public ImmutableArray<BasicBlock> BasicBlocks { get; } = basicBlocks;
    public ImmutableArray<IrEdge> Edges { get; } = edges;
    public ImmutableArray<BasicBlock> EntryBlocks { get; } = entryBlocks;

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
}

public readonly record struct IrEdge(int SourceBlockOrdinal, int TargetBlockOrdinal);
