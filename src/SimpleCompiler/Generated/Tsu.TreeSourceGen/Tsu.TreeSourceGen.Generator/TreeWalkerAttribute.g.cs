using System;

namespace Tsu.TreeSourceGen;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TreeWalkerAttribute(Type treeRoot) : Attribute
{
    public Type TreeRoot { get; } = treeRoot;
}