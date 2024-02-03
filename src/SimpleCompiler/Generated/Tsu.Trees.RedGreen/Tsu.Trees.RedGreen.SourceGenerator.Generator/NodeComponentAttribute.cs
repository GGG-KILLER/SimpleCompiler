namespace Tsu.Trees.RedGreen;

/// <summary>
/// Enabled configuring a child node's properties
/// </summary>
[global::System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
internal sealed class NodeComponentAttribute() : global::System.Attribute
{
    /// <summary>
    /// Allows the order that this child appears in prefix order (and by consequence, constructors, factories and update methods).
    /// </summary>
    public int Order { get; set; }
}