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
    public LinkedList<Instruction> Instructions { get; } = new LinkedList<Instruction>(instructions);
}

public static class InstructionListExtensions
{
    public readonly struct Enumerable(LinkedList<Instruction> instructions) : IEnumerable<LinkedListNode<Instruction>>
    {
        public Enumerator GetEnumerator() => new(instructions);
        public Reverse Reversed() => new(instructions);
        IEnumerator<LinkedListNode<Instruction>> IEnumerable<LinkedListNode<Instruction>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public readonly struct Reverse(LinkedList<Instruction> instructions) : IEnumerable<LinkedListNode<Instruction>>
        {
            public Enumerator GetEnumerator() => new(instructions);
            IEnumerator<LinkedListNode<Instruction>> IEnumerable<LinkedListNode<Instruction>>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator(LinkedList<Instruction> instructions) : IEnumerator<LinkedListNode<Instruction>>
            {
                private LinkedListNode<Instruction>? _node = instructions.Last;
                public readonly LinkedListNode<Instruction> Current => _node is null ? throw new InvalidOperationException() : _node;
                readonly object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_node?.Previous is null)
                        return false;
                    _node = _node.Previous;
                    return true;
                }
                public void Reset() => _node = instructions.Last;
                public readonly void Dispose() { }
            }
        }

        public struct Enumerator(LinkedList<Instruction> instructions) : IEnumerator<LinkedListNode<Instruction>>
        {
            private LinkedListNode<Instruction>? _node = instructions.First;
            public readonly LinkedListNode<Instruction> Current => _node is null ? throw new InvalidOperationException() : _node;
            readonly object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_node?.Next is null)
                    return false;
                _node = _node.Next;
                return true;
            }
            public void Reset() => _node = instructions.First;
            public readonly void Dispose() { }
        }
    }

    public static Enumerable Nodes(this LinkedList<Instruction> instructions) => new(instructions);

    public static LinkedListNode<Instruction>? FindLastPhi(this LinkedList<Instruction> instructions)
    {
        LinkedListNode<Instruction>? phiNode = null;
        for (var node = instructions.First; node is not null; node = node.Next)
        {
            if (node.Value.Kind == InstructionKind.PhiAssignment)
                phiNode = node;
            else
                break;
        }
        return phiNode;
    }

    public static LinkedListNode<Instruction> AppendPhi(this LinkedList<Instruction> instructions, PhiAssignment assignment)
    {
        var node = instructions.FindLastPhi();
        return node is null
            ? instructions.AddFirst(assignment)
            : instructions.AddAfter(node, assignment);
    }
}
