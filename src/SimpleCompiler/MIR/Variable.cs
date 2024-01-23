using SimpleCompiler.MIR;

namespace SimpleCompiler;

[Tsu.TreeSourceGen.TreeNode(typeof(MirNode))]
public sealed partial class Variable(VariableInfo variableInfo) : Expression
{
    public VariableInfo VariableInfo { get; } = variableInfo;

    public override IEnumerable<MirNode> GetChildren() => [];
}
