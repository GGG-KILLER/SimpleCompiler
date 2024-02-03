using Tsu.Trees.RedGreen;

namespace SimpleCompiler.MIR.Internal;

[GreenNode(MirKind.AssignmentStatement)]
internal sealed partial class AssignmentStatement : Statement
{
    [GreenList(typeof(Expression))]
    private readonly MirNode? _assignees;
    [GreenList(typeof(Expression))]
    private readonly MirNode? _values;
}
