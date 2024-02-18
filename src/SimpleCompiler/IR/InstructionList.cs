// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;

namespace SimpleCompiler.IR;

[DebuggerTypeProxy(typeof(ICollectionDebugView<>))]
[DebuggerDisplay("Count = {Count}")]
public class InstructionList : ICollection<Instruction>, ICollection, IReadOnlyCollection<Instruction>
{
    // This InstructionList is a doubly-Linked circular list.
    internal InstructionListNode? head;
    private int _count;

    public InstructionList()
    {
    }

    public InstructionList(IEnumerable<Instruction> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        foreach (Instruction item in collection)
        {
            AddLast(item);
        }
    }

    public int Count => _count;

    public InstructionListNode? First => head;

    public InstructionListNode? Last => head?.prev;

    bool ICollection<Instruction>.IsReadOnly => false;

    void ICollection<Instruction>.Add(Instruction value) => AddLast(value);

    public InstructionListNode AddAfter(InstructionListNode node, Instruction value)
    {
        ValidateNode(node);
        InstructionListNode result = new InstructionListNode(node.list!, value);
        InternalInsertNodeBefore(node.next!, result);
        return result;
    }

    public void AddAfter(InstructionListNode node, InstructionListNode newNode)
    {
        ValidateNode(node);
        ValidateNewNode(newNode);
        InternalInsertNodeBefore(node.next!, newNode);
    }

    public InstructionListNode AddBefore(InstructionListNode node, Instruction value)
    {
        ValidateNode(node);
        InstructionListNode result = new InstructionListNode(node.list!, value);
        InternalInsertNodeBefore(node, result);
        if (node == head)
        {
            head = result;
        }
        return result;
    }

    public void AddBefore(InstructionListNode node, InstructionListNode newNode)
    {
        ValidateNode(node);
        ValidateNewNode(newNode);
        InternalInsertNodeBefore(node, newNode);
        if (node == head)
        {
            head = newNode;
        }
    }

    public InstructionListNode AddFirst(Instruction value)
    {
        InstructionListNode result = new InstructionListNode(this, value);
        if (head == null)
        {
            InternalInsertNodeToEmptyList(result);
        }
        else
        {
            InternalInsertNodeBefore(head, result);
            head = result;
        }
        return result;
    }

    public void AddFirst(InstructionListNode node)
    {
        ValidateNewNode(node);

        if (head == null)
        {
            InternalInsertNodeToEmptyList(node);
        }
        else
        {
            InternalInsertNodeBefore(head, node);
            head = node;
        }
    }

    public InstructionListNode AddLast(Instruction value)
    {
        InstructionListNode result = new InstructionListNode(this, value);
        if (head == null)
        {
            InternalInsertNodeToEmptyList(result);
        }
        else
        {
            InternalInsertNodeBefore(head, result);
        }
        return result;
    }

    public void AddLast(InstructionListNode node)
    {
        ValidateNewNode(node);

        if (head == null)
        {
            InternalInsertNodeToEmptyList(node);
        }
        else
        {
            InternalInsertNodeBefore(head, node);
        }
    }

    public void Clear()
    {
        head = null;
        _count = 0;
    }

    public bool Contains(Instruction value) => Find(value) != null;

    public void CopyTo(Instruction[] array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);

        ArgumentOutOfRangeException.ThrowIfNegative(index);

        if (index > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Argument is larger than the size of the collection.");
        }

        if (array.Length - index < Count)
        {
            throw new ArgumentException("Argument has insufficient space.");
        }

        InstructionListNode? node = head;
        if (node != null)
        {
            do
            {
                array[index++] = node!.item;
                node = node.next;
            } while (node != head);
        }
    }

    public InstructionListNode? Find(Instruction value)
    {
        InstructionListNode? node = head;
        EqualityComparer<Instruction> c = EqualityComparer<Instruction>.Default;
        if (node != null)
        {
            if (value != null)
            {
                do
                {
                    if (c.Equals(node!.item, value))
                    {
                        return node;
                    }
                    node = node.next;
                } while (node != head);
            }
            else
            {
                do
                {
                    if (node!.item == null)
                    {
                        return node;
                    }
                    node = node.next;
                } while (node != head);
            }
        }
        return null;
    }

    public InstructionListNode? FindLast(Instruction value)
    {
        if (head == null) return null;

        InstructionListNode? last = head.prev;
        InstructionListNode? node = last;
        EqualityComparer<Instruction> c = EqualityComparer<Instruction>.Default;
        if (node != null)
        {
            if (value != null)
            {
                do
                {
                    if (c.Equals(node!.item, value))
                    {
                        return node;
                    }

                    node = node.prev;
                } while (node != last);
            }
            else
            {
                do
                {
                    if (node!.item == null)
                    {
                        return node;
                    }
                    node = node.prev;
                } while (node != last);
            }
        }
        return null;
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<Instruction> IEnumerable<Instruction>.GetEnumerator() => GetEnumerator();

    public bool Remove(Instruction value)
    {
        InstructionListNode? node = Find(value);
        if (node != null)
        {
            InternalRemoveNode(node);
            return true;
        }
        return false;
    }

    public void Remove(InstructionListNode node)
    {
        ValidateNode(node);
        InternalRemoveNode(node);
    }

    public void RemoveFirst()
    {
        if (head == null) { throw new InvalidOperationException("Instruction list is empty."); }
        InternalRemoveNode(head);
    }

    public void RemoveLast()
    {
        if (head == null) { throw new InvalidOperationException("Instruction list is empty."); }
        InternalRemoveNode(head.prev!);
    }

    private void InternalInsertNodeBefore(InstructionListNode node, InstructionListNode newNode)
    {
        newNode.next = node;
        newNode.prev = node.prev;
        node.prev!.next = newNode;
        node.prev = newNode;
        _count++;
    }

    private void InternalInsertNodeToEmptyList(InstructionListNode newNode)
    {
        System.Diagnostics.Debug.Assert(head == null && _count == 0, "InstructionList must be empty when this method is called!");
        newNode.next = newNode;
        newNode.prev = newNode;
        head = newNode;
        _count++;
    }

    internal void InternalRemoveNode(InstructionListNode node)
    {
        System.Diagnostics.Debug.Assert(node.list == this, "Deleting the node from another list!");
        System.Diagnostics.Debug.Assert(head != null, "This method shouldn't be called on empty list!");
        if (node.next == node)
        {
            System.Diagnostics.Debug.Assert(_count == 1 && head == node, "this should only be true for a list with only one node");
            head = null;
        }
        else
        {
            node.next!.prev = node.prev;
            node.prev!.next = node.next;
            if (head == node)
            {
                head = node.next;
            }
        }
        _count--;
    }

    internal static void ValidateNewNode(InstructionListNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.list != null)
        {
            throw new InvalidOperationException("Node is already attached to another list.");
        }
    }

    internal void ValidateNode(InstructionListNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.list != this)
        {
            throw new InvalidOperationException("Node is already linked to another list.");
        }
    }

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    void ICollection.CopyTo(Array array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);

        if (array.Rank != 1)
        {
            throw new ArgumentException("Multi-dimensional arrays are not supported.", nameof(array));
        }

        if (array.GetLowerBound(0) != 0)
        {
            throw new ArgumentException("Array does not start at zero index.", nameof(array));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(index);

        if (array.Length - index < Count)
        {
            throw new ArgumentException("Argument has insufficient space.", nameof(array));
        }

        if (array is Instruction[] tArray)
        {
            CopyTo(tArray, index);
        }
        else
        {
            // No need to use reflection to verify that the types are compatible because it isn't 100% correct and we can rely
            // on the runtime validation during the cast that happens below (i.e. we will get an ArrayTypeMismatchException).
            if (array is not object?[] objects)
            {
                throw new ArgumentException("Array has incompatible type.", nameof(array));
            }
            InstructionListNode? node = head;
            try
            {
                if (node != null)
                {
                    do
                    {
                        objects[index++] = node!.item;
                        node = node.next;
                    } while (node != head);
                }
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("Array has incompatible type.", nameof(array));
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<Instruction>) this).GetEnumerator();

    public struct Enumerator : IEnumerator<Instruction>, IEnumerator
    {
        private readonly InstructionList _list;
        private InstructionListNode? _node;
        private Instruction? _current;
        private int _index;

        internal Enumerator(InstructionList list)
        {
            _list = list;
            _node = list.head;
            _current = default;
            _index = 0;
        }

        public readonly Instruction Current => _current!;

        object? IEnumerator.Current
        {
            get
            {
                if (_index == 0 || (_index == _list.Count + 1))
                {
                    throw new InvalidOperationException("Move next hasn't been called or its return not respected.");
                }

                return Current;
            }
        }

        public bool MoveNext()
        {
            if (_node == null)
            {
                _index = _list.Count + 1;
                return false;
            }

            ++_index;
            _current = _node.item;
            _node = _node.next;
            if (_node == _list.head)
            {
                _node = null;
            }
            return true;
        }

        void IEnumerator.Reset()
        {
            _current = default;
            _node = _list.head;
            _index = 0;
        }

        public readonly void Dispose()
        {
        }
    }
}

// Note following class is not serializable since we customized the serialization of InstructionList.
public sealed class InstructionListNode
{
    internal readonly InstructionList? list;
    internal InstructionListNode? next;
    internal InstructionListNode? prev;
    internal Instruction item;

    internal InstructionListNode(InstructionList list, Instruction value)
    {
        this.list = list;
        item = value;
    }

    public InstructionList? List => list;

    public InstructionListNode? Next => next == null || next == list!.head ? null : next;

    public InstructionListNode? Previous => prev == null || this == list!.head ? null : prev;

    public Instruction Value { get => item; set => item = value; }

    /// <summary>Gets a reference to the value held by the node.</summary>
    public ref Instruction ValueRef => ref item;
}

internal sealed class ICollectionDebugView<T>
{
    private readonly ICollection<T> _collection;

    public ICollectionDebugView(ICollection<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        _collection = collection;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
        get
        {
            T[] items = new T[_collection.Count];
            _collection.CopyTo(items, 0);
            return items;
        }
    }
}
