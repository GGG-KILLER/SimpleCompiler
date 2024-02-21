using SimpleCompiler.Helpers;
using Tsu.Buffers;

namespace SimpleCompiler.IR;

public sealed class Worklist(int capacity)
{
    private readonly byte[] _bitset = new byte[MathEx.RoundUpDivide(capacity, 8)];
    private readonly Queue<int> _queue = new();

    public void Add(int blockOrdinal)
    {
        if (BitVectorHelpers.GetByteVectorBitValue(_bitset.AsSpan(), blockOrdinal))
            return;

        BitVectorHelpers.SetByteVectorBitValue(_bitset.AsSpan(), blockOrdinal, true);
        _queue.Enqueue(blockOrdinal);
    }

    public bool TryGetNext(out int blockOrdinal)
    {
        if (_queue.TryDequeue(out blockOrdinal))
        {
            BitVectorHelpers.SetByteVectorBitValue(_bitset.AsSpan(), blockOrdinal, false);
            return true;
        }

        blockOrdinal = default;
        return false;
    }
}

public readonly ref struct StackWorklist(IrGraph graph, Span<byte> bitVec)
{
    private readonly Span<byte> _bitVec = bitVec;

    public void MarkPredecessors(int blockOrdinal, bool isClean)
    {
        foreach (var ordinal in graph.Edges.GetPredecessors(blockOrdinal))
        {
            SetClean(ordinal, isClean);
        }
    }

    public void MarkSuccessors(int blockOrdinal, bool isClean)
    {
        foreach (var ordinal in graph.Edges.GetSuccessors(blockOrdinal))
        {
            SetClean(ordinal, isClean);
        }
    }

    public bool IsClean(int ordinal) => BitVectorHelpers.GetByteVectorBitValue(_bitVec, ordinal);
    public void SetClean(int ordinal, bool isClean) => BitVectorHelpers.SetByteVectorBitValue(_bitVec, ordinal, isClean);
}
