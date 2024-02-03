namespace Tsu.Trees.RedGreen;

/// <summary>
/// Marks this class as a green node.
/// </summary>
/// <param name="kinds">
/// The kinds this node could have.
/// This should be the value on the enum (e.g.: SyntaxKind.ClassDeclarationSyntax)
/// </param>
[global::System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class GreenNodeAttribute(params object[] kinds) : global::System.Attribute
{
    /// <summary>
    /// This node's Kind.
    /// </summary>
    public object[] Kinds { get; } = kinds;
}