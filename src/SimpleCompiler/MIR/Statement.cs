namespace SimpleCompiler.MIR;

[Tsu.TreeSourceGen.TreeNode(typeof(MirNode))]
public abstract partial class Statement : MirNode
{
}

[Tsu.TreeSourceGen.TreeNode(typeof(MirNode))]
public sealed partial class EmptyStatement : Statement
{
    public override IEnumerable<MirNode> GetChildren() => [];
}