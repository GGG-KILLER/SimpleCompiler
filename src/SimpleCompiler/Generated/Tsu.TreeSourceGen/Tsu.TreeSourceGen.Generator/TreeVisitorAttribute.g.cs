using System;

namespace Tsu.TreeSourceGen;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TreeVisitorAttribute(Type treeRoot) : Attribute
{
    public Type TreeRoot { get; } = treeRoot;
}