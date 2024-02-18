using System.Collections;

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
    public InstructionList Instructions { get; } = new InstructionList(instructions);

    public BasicBlock Clone() => new(Ordinal, [.. Instructions.Select(x => x.Clone())]);
}

public static class InstructionListExtensions
{
    public readonly struct Enumerable(InstructionList instructions) : IEnumerable<InstructionListNode>
    {
        public Enumerator GetEnumerator() => new(instructions);
        public Reverse Reversed() => new(instructions);
        IEnumerator<InstructionListNode> IEnumerable<InstructionListNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public readonly struct Reverse(InstructionList instructions) : IEnumerable<InstructionListNode>
        {
            public Enumerator GetEnumerator() => new(instructions);
            IEnumerator<InstructionListNode> IEnumerable<InstructionListNode>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator(InstructionList instructions) : IEnumerator<InstructionListNode>
            {
                private bool _first = true;
                private InstructionListNode? _node = instructions.Last;
                public readonly InstructionListNode Current => _node is null ? throw new InvalidOperationException() : _node;
                readonly object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if ((_first ? _node : _node?.Previous) is null)
                        return false;
                    if (!_first)
                        _node = _node!.Previous;
                    _first = false;
                    return true;
                }
                public void Reset() => (_first, _node) = (true, instructions.Last);
                public readonly void Dispose() { }
            }
        }

        public struct Enumerator(InstructionList instructions) : IEnumerator<InstructionListNode>
        {
            private bool _first = true;
            private InstructionListNode? _node = instructions.First;
            public readonly InstructionListNode Current => _node is null ? throw new InvalidOperationException() : _node;
            readonly object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if ((_first ? _node : _node?.Next) is null)
                    return false;
                if (!_first)
                    _node = _node!.Next;
                _first = false;
                return true;
            }
            public void Reset() => (_first, _node) = (true, instructions.First);
            public readonly void Dispose() { }
        }
    }

    public static Enumerable Nodes(this InstructionList instructions) => new(instructions);

    public static InstructionListNode? FindLastPhi(this InstructionList instructions)
    {
        InstructionListNode? phiNode = null;
        for (var node = instructions.First; node is not null; node = node.Next)
        {
            if (node.Value.Kind == InstructionKind.PhiAssignment)
                phiNode = node;
            else
                break;
        }
        return phiNode;
    }

    public static InstructionListNode AppendPhi(this InstructionList instructions, PhiAssignment assignment)
    {
        var node = instructions.FindLastPhi();
        return node is null
            ? instructions.AddFirst(assignment)
            : instructions.AddAfter(node, assignment);
    }
}
