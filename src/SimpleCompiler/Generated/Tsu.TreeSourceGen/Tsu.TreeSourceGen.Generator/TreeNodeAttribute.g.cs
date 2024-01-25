using System;

namespace Tsu.TreeSourceGen;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TreeNodeAttribute(Type treeRoot) : Attribute
{
    public Type TreeRoot { get; } = treeRoot;

    public string Name { get; set; }
}