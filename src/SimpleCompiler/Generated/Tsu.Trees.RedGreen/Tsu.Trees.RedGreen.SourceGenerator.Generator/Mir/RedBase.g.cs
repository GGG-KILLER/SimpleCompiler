﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Modified by the Tsu (https://github.com/GGG-KILLER/Tsu) project for embedding into other projects.
// <auto-generated />

#nullable enable

namespace SimpleCompiler.MIR
{
    abstract partial class MirNode
    {
        private readonly global::SimpleCompiler.MIR.MirNode? _parent;

        private protected MirNode(global::SimpleCompiler.MIR.Internal.MirNode green, global::SimpleCompiler.MIR.MirNode? parent)
        {
            this._parent = parent;
            this.Green = green;
        }

        public global::SimpleCompiler.MIR.MirKind Kind => this.Green.Kind;
        internal global::SimpleCompiler.MIR.Internal.MirNode Green { get; }
        public global::SimpleCompiler.MIR.MirNode? Parent => _parent;
        internal int SlotCount => this.Green.SlotCount;
        internal bool IsList => this.Green.IsList;

        protected T? GetRed<T>(ref T? field, int index) where T : global::SimpleCompiler.MIR.MirNode
        {
            var result = field;

            if (result == null)
            {
                var green = this.Green.GetSlot(index);
                if (green != null)
                {
                    global::System.Threading.Interlocked.CompareExchange(ref field, (T) green.CreateRed(this), null);
                    result = field;
                }
            }

            return result;
        }

        /// <summary>
        /// This works the same as GetRed, but intended to be used in lists
        /// The only difference is that the public parent of the node is not the list,
        /// but the list's parent. (element's grand parent).
        /// </summary>
        protected global::SimpleCompiler.MIR.MirNode? GetRedElement(ref global::SimpleCompiler.MIR.MirNode? element, int slot)
        {
            global::System.Diagnostics.Debug.Assert(IsList);

            var result = element;

            if (result == null)
            {
                var green = Green.GetRequiredSlot(slot);
                // passing list's parent
                global::System.Threading.Interlocked.CompareExchange(ref element, green.CreateRed(Parent), null);
                result = element;
            }

            return result;
        }

        public bool IsEquivalentTo(global::SimpleCompiler.MIR.MirNode? other)
        {
            if (this == other) return true;
            if (other == null) return false;

            return this.Green.IsEquivalentTo(other.Green);
        }

        public bool Contains(global::SimpleCompiler.MIR.MirNode other)
        {
            for (var node = other; node != null; node = node.Parent)
            {
                if (node == this)
                    return true;
            }

            return false;
        }

        internal abstract global::SimpleCompiler.MIR.MirNode? GetNodeSlot(int index);

        internal global::SimpleCompiler.MIR.MirNode GetRequiredNodeSlot(int index)
        {
            var node = this.GetNodeSlot(index);
            global::System.Diagnostics.Debug.Assert(node != null);
            return node!;
        }

        public abstract void Accept(global::SimpleCompiler.MIR.MirVisitor visitor);
        public abstract TResult? Accept<TResult>(global::SimpleCompiler.MIR.MirVisitor<TResult> visitor);
        public abstract TResult? Accept<T1, TResult>(global::SimpleCompiler.MIR.MirVisitor<T1, TResult> visitor, T1 arg1);
        public abstract TResult? Accept<T1, T2, TResult>(global::SimpleCompiler.MIR.MirVisitor<T1, T2, TResult> visitor, T1 arg1, T2 arg2);
        public abstract TResult? Accept<T1, T2, T3, TResult>(global::SimpleCompiler.MIR.MirVisitor<T1, T2, T3, TResult> visitor, T1 arg1, T2 arg2, T3 arg3);

        public global::System.Collections.Generic.IEnumerable<global::SimpleCompiler.MIR.MirNode> ChildNodes()
        {
            var count = this.SlotCount;
            for (var index = 0; index < count; index++)
                yield return this.GetRequiredNodeSlot(index);
        }

        public global::System.Collections.Generic.IEnumerable<global::SimpleCompiler.MIR.MirNode> Ancestors() =>
            this.Parent?.AncestorsAndSelf() ?? global::System.Linq.Enumerable.Empty<global::SimpleCompiler.MIR.MirNode>();

        public global::System.Collections.Generic.IEnumerable<global::SimpleCompiler.MIR.MirNode> AncestorsAndSelf()
        {
            for (var node = this; node != null; node = node.Parent)
                yield return node;
        }

        public TNode? FirstAncestorOrSelf<TNode>(Func<TNode, bool>? predicate = null) where TNode : global::SimpleCompiler.MIR.MirNode
        {
            for (var node = this; node != null; node = node.Parent)
            {
                if (node is TNode tnode && (predicate == null || predicate(tnode)))
                    return tnode;
            }

            return null;
        }

        public TNode? FirstAncestorOrSelf<TNode, TArg>(Func<TNode, TArg, bool> predicate, TArg argument) where TNode : global::SimpleCompiler.MIR.MirNode
        {
            for (var node = this; node != null; node = node.Parent)
            {
                if (node is TNode tnode && (predicate == null || predicate(tnode, argument)))
                    return tnode;
            }

            return null;
        }

        public global::System.Collections.Generic.IEnumerable<global::SimpleCompiler.MIR.MirNode> DescendantNodes(Func<global::SimpleCompiler.MIR.MirNode, bool>? descendIntoChildren = null)
        {
            var stack = new Stack<global::SimpleCompiler.MIR.MirNode>(24);
            foreach (var child in this.ChildNodes())
                stack.Push(child);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                yield return current;

                foreach (var child in current.ChildNodes().Reverse())
                {
                    stack.Push(child);
                }
            }
        }

        public global::System.Collections.Generic.IEnumerable<global::SimpleCompiler.MIR.MirNode> DescendantNodesAndSelf(Func<global::SimpleCompiler.MIR.MirNode, bool>? descendIntoChildren = null)
        {
            var stack = new Stack<global::SimpleCompiler.MIR.MirNode>(24);
            stack.Push(this);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                yield return current;

                foreach (var child in current.ChildNodes().Reverse())
                {
                    stack.Push(child);
                }
            }
        }
    }
}
