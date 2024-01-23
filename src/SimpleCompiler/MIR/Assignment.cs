namespace SimpleCompiler.MIR;

[Tsu.TreeSourceGen.TreeNode(typeof(MirNode))]
public sealed partial class Assignment : Statement
{
    public Expression Assignee { get; }
    public Expression Value { get; }

    public Assignment(Expression assignee, Expression value)
    {
        Assignee = assignee;
        Assignee.Parent = this;
        Value = value;
        Value.Parent = this;
    }

    public override IEnumerable<MirNode> GetChildren() => [Assignee, Value];
}
