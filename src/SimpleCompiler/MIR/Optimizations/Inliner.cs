using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using SimpleCompiler.Helpers;

namespace SimpleCompiler.MIR.Optimizations;

internal sealed class Inliner : MirRewriter
{
    private readonly Dictionary<VariableInfo, (Expression Original, Expression Replacement)> _values = [];

    [return: NotNullIfNotNull(nameof(node))]
    public override MirNode? Visit(MirNode? node) => base.Visit(node);

    public override MirNode VisitAssignmentStatement(AssignmentStatement node)
    {
        if (node.Assignees.OfType<VariableExpression>().Any(x => x.VariableInfo.Writes.Count == 1))
        {
            var assignees = new MirListBuilder<Expression>(node.Assignees.Count);
            var values = new MirListBuilder<Expression>(node.Values.Count);
            for (var idx = 0; idx < node.Assignees.Count; idx++)
            {
                var var = node.Assignees[idx];
                var val = (Expression) Visit(node.Values[idx])!;
                if (var is not VariableExpression { VariableInfo.Writes.Count: 1 } || !val.IsConstant())
                {
                    assignees.Add((Expression) Visit(var)!);
                    values.Add(val);
                }
                else
                {
                    _values.Add(((VariableExpression) var).VariableInfo, (node.Values[idx], val));
                }
            }

            if (assignees.Count < 1)
                return MirFactory.None;

            return node.Update(node.OriginalNode, assignees.ToList(), values.ToList());
        }
        return base.VisitAssignmentStatement(node);
    }

    public override MirNode VisitVariableExpression(VariableExpression node)
    {
        if (_values.TryGetValue(node.VariableInfo, out var value))
        {
#if DEBUG
            if (value.Original.IsBeforeInPrefixOrder(node))
            {
#endif // DEBUG
                return value.Replacement;
#if DEBUG
            }
            else
            {
                throw new UnreachableException("Somehow variable reference is before declaration.");
            }
#endif // DEBUG
        }

        return base.VisitVariableExpression(node);
    }
}
