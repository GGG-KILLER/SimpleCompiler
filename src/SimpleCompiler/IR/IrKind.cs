namespace SimpleCompiler.IR;

public enum IrKind
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
