using System.Collections.Immutable;

namespace SimpleCompiler.MIR;

[Tsu.TreeSourceGen.TreeNode(typeof(MirNode))]
public sealed partial class FunctionCall : Expression
{
    public Expression Callee { get; }
    public ImmutableArray<Expression> Arguments { get; }

    public FunctionCall(Expression callee, IEnumerable<Expression> arguments)
    {
        Callee = callee;
        Callee.Parent = this;
        Arguments = arguments.ToImmutableArray();
        foreach (var arg in Arguments) arg.Parent = this;
    }

    public override IEnumerable<MirNode> GetChildren() => Arguments.Prepend(Callee);
}
