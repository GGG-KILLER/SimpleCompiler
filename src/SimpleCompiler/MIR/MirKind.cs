namespace SimpleCompiler.MIR;

public enum MirKind
{
    None,
    List,

    ConstantExpression,
    VariableExpression,
    DiscardExpression,
    UnaryOperationExpression,
    BinaryOperationExpression,
    FunctionCallExpression,

    AssignmentStatement,
    ExpressionStatement,
    EmptyStatement,
    StatementList,
}