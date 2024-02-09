namespace SimpleCompiler.IR;

public static class IrNavigator
{
    public static IrNode? GetCommonAncestor(this IrNode node, IrNode other) =>
        node.AncestorsAndSelf().Intersect(other.AncestorsAndSelf()).LastOrDefault();

    public static bool IsBeforeInPrefixOrder(this IrNode node, IrNode other)
    {
        var parent = node.GetCommonAncestor(other);
        if (parent is null)
            return false;

        var currentIndex = 0;
        var nodeIndex = -1;
        var otherIndex = -1;
        foreach (var curr in parent.DescendantNodesAndSelf())
        {
            if (curr == node)
                nodeIndex = currentIndex;
            if (curr == other)
                otherIndex = currentIndex;
            if (nodeIndex != -1 && otherIndex != -1)
                break;
            currentIndex++;
        }

        return nodeIndex != -1 && nodeIndex <= otherIndex;
    }
}
