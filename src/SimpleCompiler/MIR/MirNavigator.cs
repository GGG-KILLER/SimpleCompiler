namespace SimpleCompiler.MIR;

public static class MirNavigator
{
    public static MirNode? GetCommonAncestor(this MirNode node, MirNode other) =>
        node.AncestorsAndSelf().Intersect(other.AncestorsAndSelf()).LastOrDefault();

    public static bool IsLocatedBefore(this MirNode node, MirNode other)
    {
        // Only go up a level because everything other than synthesized constants shouldn't have a parent.
        var nodeOriginal = node.OriginalNode ?? node.Parent?.OriginalNode;
        var otherOriginal = other.OriginalNode ?? other.Parent?.OriginalNode;

        if (nodeOriginal is not null && otherOriginal is not null)
            return nodeOriginal.Span.CompareTo(otherOriginal.Span) <= 0;
        return false;
    }
}
