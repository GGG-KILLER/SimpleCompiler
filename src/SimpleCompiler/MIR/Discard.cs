
namespace SimpleCompiler.MIR;

[Tsu.TreeSourceGen.TreeNode(typeof(MirNode))]
public partial class Discard : Expression
{
    public override IEnumerable<MirNode> GetChildren() => [];
}
