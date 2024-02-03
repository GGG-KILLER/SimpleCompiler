using Tsu.Trees.RedGreen;

namespace SimpleCompiler.MIR.Internal;

[GreenNode(MirKind.VariableExpression)]
internal sealed partial class VariableExpression : Expression
{
    private readonly VariableInfo _variableInfo;
}
