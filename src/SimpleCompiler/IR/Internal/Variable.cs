using Tsu.Trees.RedGreen;

namespace SimpleCompiler.IR.Internal;

[GreenNode(IrKind.VariableExpression)]
internal sealed partial class VariableExpression : Expression
{
    private readonly VariableInfo _variableInfo;
}
