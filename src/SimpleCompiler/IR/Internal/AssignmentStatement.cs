using Tsu.Trees.RedGreen;

namespace SimpleCompiler.IR.Internal;

[GreenNode(IrKind.AssignmentStatement)]
internal sealed partial class AssignmentStatement : Statement
{
    [GreenList(typeof(Expression))]
    private readonly IrNode? _assignees;
    [GreenList(typeof(Expression))]
    private readonly IrNode? _values;
}
