using System.Collections.Immutable;

namespace SimpleCompiler.MIR;

[Tsu.TreeSourceGen.TreeNode(typeof(MirNode))]
public sealed partial class Assignment : Statement
{
    public ImmutableArray<Expression> Assignees { get; }
    public ImmutableArray<Expression> Values { get; }

    public Assignment(IEnumerable<Expression> assignees, IEnumerable<Expression> values)
    {
        Assignees = assignees.ToImmutableArray();
        foreach (var assignee in Assignees) assignee.Parent = this;
        Values = values.ToImmutableArray();
        foreach (var value in Values) value.Parent = this;
    }

    public override IEnumerable<MirNode> GetChildren() => [.. Assignees, .. Values];
}
