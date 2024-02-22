using SimpleCompiler.Helpers;
using Tsu.Buffers;

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
        BasicBlocks.SelectMany(x => x.Instructions).Single(x => x.IsAssignment && x.Name == name);

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

public static class IrGraphExtensions
{
    public static IEnumerable<int> GetPredecessors(this IReadOnlyList<IrEdge> edges, int blockOrdinal) =>
        edges.Where(x => x.TargetBlockOrdinal == blockOrdinal).Select(x => x.SourceBlockOrdinal);

    public static IEnumerable<int> GetSuccessors(this IReadOnlyList<IrEdge> edges, int blockOrdinal) =>
        edges.Where(x => x.SourceBlockOrdinal == blockOrdinal).Select(x => x.TargetBlockOrdinal);

    public static IEnumerable<int> EnumerateBlocksBreadthFirst(this IrGraph graph)
    {
        var visitedSet = new byte[MathEx.RoundUpDivide(graph.BasicBlocks.Count, 8)];
        Array.Fill(visitedSet, (byte) 0);

        var queue = new Queue<int>(graph.BasicBlocks.Count);
        queue.Enqueue(graph.EntryBlock.Ordinal);

        while (queue.TryDequeue(out var blockOrdinal))
        {
            if (BitVectorHelpers.GetByteVectorBitValue(visitedSet.AsSpan(), blockOrdinal))
                continue;
            BitVectorHelpers.SetByteVectorBitValue(visitedSet.AsSpan(), blockOrdinal, true);

            foreach (var successor in graph.Edges.GetSuccessors(blockOrdinal))
                queue.Enqueue(successor);

            yield return blockOrdinal;
        }
    }
}
