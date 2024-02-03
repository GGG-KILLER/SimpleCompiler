namespace Tsu.Trees.RedGreen;

/// <summary>
/// Marks this field as a green list of nodes of the specified type.
/// </summary>
[global::System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
internal sealed class GreenListAttribute(global::System.Type elementType) : global::System.Attribute
{
    /// <summary>
    /// The type of element in this List.
    /// </summary>
    public global::System.Type ElementType { get; } = elementType;
}