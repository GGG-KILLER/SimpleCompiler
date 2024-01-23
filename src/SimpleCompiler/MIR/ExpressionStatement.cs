namespace SimpleCompiler.MIR;

[Tsu.TreeSourceGen.TreeNode(typeof(MirNode))]
public sealed partial class ExpressionStatement : Statement
{
    public Expression Expression { get; }

    public ExpressionStatement(Expression expression)
    {
        Expression = expression;
        Expression.Parent = this;
    }

    public override IEnumerable<MirNode> GetChildren() => [Expression];
}
