﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Modified by the Tsu (https://github.com/GGG-KILLER/Tsu) project for embedding into other projects.
// <auto-generated />

#nullable enable

namespace SimpleCompiler.MIR
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// A list containing all children of a Mir node.
    /// </summary>
    public readonly partial struct ChildMirList : IEquatable<ChildMirList>, IReadOnlyList<global::SimpleCompiler.MIR.MirNode>
    {
        private readonly global::SimpleCompiler.MIR.MirNode? _node;
        private readonly int _count;

        internal ChildMirList(global::SimpleCompiler.MIR.MirNode node)
        {
            _node = node;
            _count = CountNodes(node.Green);
        }

        /// <summary>
        /// Gets the number of children contained in the <see cref="ChildMirList"/>.
        /// </summary>
        public int Count => _count;

        internal static int CountNodes(global::SimpleCompiler.MIR.Internal.MirNode green)
        {
            int n = 0;

            for (int i = 0, s = green.SlotCount; i < s; i++)
            {
                var child = green.GetSlot(i);
                if (child != null)
                {
                    if (!child.IsList)
                    {
                        n++;
                    }
                    else
                    {
                        n += child.SlotCount;
                    }
                }
            }

            return n;
        }

        /// <summary>Gets the child at the specified index.</summary>
        /// <param name="index">The zero-based index of the child to get.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///   <paramref name="index"/> is less than 0.-or-<paramref name="index" /> is equal to or greater than <see cref="ChildMirList.Count"/>. </exception>
        public global::SimpleCompiler.MIR.MirNode this[int index]
        {
            get
            {
                if (unchecked((uint) index < (uint) _count))
                {
                    return ItemInternal(_node!, index);
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        internal global::SimpleCompiler.MIR.MirNode? Node => _node;

        private static int Occupancy(global::SimpleCompiler.MIR.Internal.MirNode green) => green.IsList ? green.SlotCount : 1;

        /// <summary>
        /// internal indexer that does not verify index.
        /// Used when caller has already ensured that index is within bounds.
        /// </summary>
        internal static global::SimpleCompiler.MIR.MirNode ItemInternal(global::SimpleCompiler.MIR.MirNode node, int index)
        {
            global::SimpleCompiler.MIR.Internal.MirNode? greenChild;
            var green = node.Green;
            var idx = index;
            var slotIndex = 0;

            // find a slot that contains the node or its parent list (if node is in a list)
            // we will be skipping whole slots here so we will not loop for long (hopefully)
            //
            // at the end of this loop we will have
            // 1) slot index - slotIdx
            // 2) if the slot is a list, node index in the list - idx
            while (true)
            {
                greenChild = green.GetSlot(slotIndex);
                if (greenChild != null)
                {
                    int currentOccupancy = Occupancy(greenChild);
                    if (idx < currentOccupancy)
                    {
                        break;
                    }

                    idx -= currentOccupancy;
                }

                slotIndex++;
            }

            // get node that represents this slot
            var red = node.GetRequiredNodeSlot(slotIndex);
            if (greenChild.IsList)
            {
                // it is a red list of nodes, most common case
                return red.GetRequiredNodeSlot(idx);
            }

            // this is a single node
            return red;
        }

#if DEBUG
        [Obsolete("For debugging only", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "For debugging only")]
        private global::SimpleCompiler.MIR.MirNode[] Nodes => this.ToArray();
#endif

        /// <summary>
        /// Checks whether this list contains any elements.
        /// </summary>
        /// <returns></returns>
        public bool Any() => _count != 0;

        /// <summary>
        /// Returns the first child in the list.
        /// </summary>
        /// <returns>The first child in the list.</returns>
        /// <exception cref="System.InvalidOperationException">The list is empty.</exception>
        public global::SimpleCompiler.MIR.MirNode First()
        {
            if (Any())
            {
                return this[0];
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Returns the last child in the list.
        /// </summary>
        /// <returns>The last child in the list.</returns>
        /// <exception cref="System.InvalidOperationException">The list is empty.</exception>
        public global::SimpleCompiler.MIR.MirNode Last()
        {
            if (Any())
            {
                return this[_count - 1];
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Returns a list which contains all children of <see cref="ChildMirList"/> in reversed order.
        /// </summary>
        /// <returns><see cref="Reversed"/> which contains all children of <see cref="ChildMirList"/> in reversed order</returns>
        public Reversed Reverse()
        {
            global::System.Diagnostics.Debug.Assert(_node is not null);
            return new Reversed(_node, _count);
        }

        /// <summary>Returns an enumerator that iterates through the <see cref="ChildMirList"/>.</summary>
        /// <returns>A <see cref="Enumerator"/> for the <see cref="ChildMirList"/>.</returns>
        public Enumerator GetEnumerator()
        {
            if (_node == null)
            {
                return default;
            }

            return new Enumerator(_node, _count);
        }

        IEnumerator<global::SimpleCompiler.MIR.MirNode> IEnumerable<global::SimpleCompiler.MIR.MirNode>.GetEnumerator()
        {
            return new EnumeratorImpl(_node, _count);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new EnumeratorImpl(_node, _count);
        }

        /// <summary>Determines whether the specified object is equal to the current instance.</summary>
        /// <returns>true if the specified object is a <see cref="ChildMirList" /> structure and is equal to the current instance; otherwise, false.</returns>
        /// <param name="obj">The object to be compared with the current instance.</param>
        public override bool Equals(object? obj) =>
            obj is ChildMirList list && Equals(list);

        /// <summary>Determines whether the specified <see cref="ChildMirList" /> structure is equal to the current instance.</summary>
        /// <returns>true if the specified <see cref="ChildMirList" /> structure is equal to the current instance; otherwise, false.</returns>
        /// <param name="other">The <see cref="ChildMirList" /> structure to be compared with the current instance.</param>
        public bool Equals(ChildMirList other) => _node == other._node;

        /// <summary>Returns the hash code for the current instance.</summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => _node?.GetHashCode() ?? 0;

        /// <summary>Indicates whether two <see cref="ChildMirList" /> structures are equal.</summary>
        /// <returns>true if <paramref name="list1" /> is equal to <paramref name="list2" />; otherwise, false.</returns>
        /// <param name="list1">The <see cref="ChildMirList" /> structure on the left side of the equality operator.</param>
        /// <param name="list2">The <see cref="ChildMirList" /> structure on the right side of the equality operator.</param>
        public static bool operator ==(ChildMirList list1, ChildMirList list2) =>
            list1.Equals(list2);

        /// <summary>Indicates whether two <see cref="ChildMirList" /> structures are unequal.</summary>
        /// <returns>true if <paramref name="list1" /> is equal to <paramref name="list2" />; otherwise, false.</returns>
        /// <param name="list1">The <see cref="ChildMirList" /> structure on the left side of the inequality operator.</param>
        /// <param name="list2">The <see cref="ChildMirList" /> structure on the right side of the inequality operator.</param>
        public static bool operator !=(ChildMirList list1, ChildMirList list2) =>
            !list1.Equals(list2);
    }
}