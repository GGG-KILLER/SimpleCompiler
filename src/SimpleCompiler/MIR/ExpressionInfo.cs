namespace SimpleCompiler.MIR;

public static class ExpressionExtensions
{
    public static bool IsConstant(this Expression? expression) =>
        IsConstVisitor.s_intance.Visit(expression);

    public static bool IsAssignee(this Expression? expression)
    {
        if (expression?.Parent is null)
            return false;

        return expression.Parent is AssignmentStatement assignment
            && assignment.Assignees.Contains(expression);
    }

    private sealed class IsConstVisitor : MirVisitor<bool>
    {
        public static readonly IsConstVisitor s_intance = new();

        public override bool VisitDiscardExpression(DiscardExpression node) => true;
        public override bool VisitConstantExpression(ConstantExpression node) => true;
        public override bool VisitFunctionCallExpression(FunctionCallExpression node) => false;
        public override bool VisitVariableExpression(VariableExpression node) => false;

        public override bool VisitBinaryOperationExpression(BinaryOperationExpression node) =>
            Visit(node.Left) && Visit(node.Right);
        public override bool VisitUnaryOperationExpression(UnaryOperationExpression node) => Visit(node.Operand);

        protected override bool DefaultVisit(MirNode node) => false;
    }
}
