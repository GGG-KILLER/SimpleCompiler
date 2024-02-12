using Tsu.Trees.RedGreen;

namespace SimpleCompiler.IR.Internal;

[GreenNode(IrKind.IfStatement)]
internal sealed partial class IfStatement : Statement
{
    [GreenList(typeof(IfClause))]
    private readonly IrNode? _clauses;
    private readonly StatementList? _elseBody;
}

[GreenNode(IrKind.IfClause)]
internal sealed partial class IfClause : IrNode
{
    private readonly Expression _condition;
    private readonly StatementList _body;
}
