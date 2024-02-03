namespace Tsu.Trees.RedGreen;

/// <summary>
/// An attribute that marks the given class as the base class for all nodes in a green node tree.
/// </summary>
[global::System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class GreenTreeRootAttribute(Type redBase, string suffix, Type kindEnum) : global::System.Attribute
{
    /// <summary>
    /// The suffix for nodes in this tree.
    /// </summary>
    public string Suffix { get; } = suffix;

    /// <summary>
    /// The base node type for all nodes in the red tree.
    /// </summary>
    public global::System.Type RedBase { get; } = redBase;

    /// <summary>
    /// The enum type that contains the definitions for the node kinds.
    /// </summary>
    public global::System.Type KindEnum { get; } = kindEnum;

    /// <summary>
    /// Whether to create base visitor implementations for this tree.
    /// </summary>
    public bool CreateVisitors { get; set; }

    /// <summary>
    /// Whether to create a base walker implementation for this tree.
    /// </summary>
    public bool CreateWalker { get; set; }

    /// <summary>
    /// Whether to generate a base rewriter implementation for this tree.
    /// </summary>
    public bool CreateRewriter { get; set; }

    /// <summary>
    /// Whether to generate list types and infrastructure for this tree.
    /// This implies in having roslyn MIT-licensed code being added to your project.
    /// </summary>
    public bool CreateLists { get; set; }

    /// <summary>
    /// Whether to generate a file with only comments dumping the entire tree structure.
    /// </summary>
    public bool DebugDump { get; set; }
}