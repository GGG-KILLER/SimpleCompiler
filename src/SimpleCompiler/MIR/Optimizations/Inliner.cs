using System.Diagnostics.CodeAnalysis;
using SimpleCompiler.MIR.Ssa;

namespace SimpleCompiler.MIR.Optimizations;

internal sealed class Inliner(MirTree tree) : MirRewriter
{
    private readonly IsConstantVisitor _isConstant = new(tree);

    [return: NotNullIfNotNull(nameof(node))]
    public override MirNode? Visit(MirNode? node) => base.Visit(node);

    public override MirNode VisitAssignmentStatement(AssignmentStatement node)
    {
        if (node.Assignees.OfType<VariableExpression>().Any(x => CanInline(x, out _)))
        {
            var assignees = new MirListBuilder<Expression>(node.Assignees.Count);
            var values = new MirListBuilder<Expression>(node.Values.Count);
            for (var idx = 0; idx < node.Assignees.Count; idx++)
            {
                var var = node.Assignees[idx];
                var val = (Expression) Visit(node.Values[idx])!;

                // Do not add discards that have constant values.
                if (var is DiscardExpression && _isConstant.Visit(node.Values[idx]))
                    continue;

                // Remove variables that will be inlined.
                if (var is VariableExpression variable && CanInline(variable, out _))
                    continue;

                assignees.Add(var is VariableExpression ? var : (Expression) Visit(var)!);
                values.Add(val);
            }

            // Remove node if there are no assignments left.
            if (assignees.Count < 1)
                return MirFactory.None;

            return node.Update(node.OriginalNode, assignees.ToList(), values.ToList());
        }

        return base.VisitAssignmentStatement(node);
    }

    public override MirNode VisitVariableExpression(VariableExpression node)
    {
        if (CanInline(node, out var value) && !node.IsAssignee())
        {
            return Visit(value.Value!);
        }

        return base.VisitVariableExpression(node);
    }

    private bool CanInline(VariableExpression variable, [NotNullWhen(true)] out SsaValueVersion? version) =>
        CanInline(version = tree.Ssa.GetVariableVersion(variable));

    private bool CanInline([NotNullWhen(true)] SsaValueVersion? value) =>
        value is not null && !value.IsPhi && _isConstant.Visit(value.Value);

    private class IsConstantVisitor(MirTree tree) : MirVisitor<bool>
    {
        public override bool VisitDiscardExpression(DiscardExpression node) => true;
        public override bool VisitConstantExpression(ConstantExpression node) => true;
        public override bool VisitFunctionCallExpression(FunctionCallExpression node) => false;
        public override bool VisitVariableExpression(VariableExpression node)
        {
            if (tree.Ssa.GetVariableVersion(node) is { } value && !value.IsPhi)
            {
                return Visit(value.Value);
            }

            return false;
        }

        public override bool VisitBinaryOperationExpression(BinaryOperationExpression node) =>
            Visit(node.Left) && Visit(node.Right);
        public override bool VisitUnaryOperationExpression(UnaryOperationExpression node) => Visit(node.Operand);

        protected override bool DefaultVisit(MirNode node) => false;
    }
}
