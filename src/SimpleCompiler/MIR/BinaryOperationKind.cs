namespace SimpleCompiler.MIR;

public enum BinaryOperationKind
{
    Addition,
    Subtraction,
    Multiplication,
    Division,
    IntegerDivision,
    Exponentiation,
    Modulo,
    Concatenation,

    BooleanAnd,
    BooleanOr,
    BitwiseAnd,
    BitwiseOr,
    BitwiseXor,
    LeftShift,
    RightShift,

    Equals,
    NotEquals,
    LessThan,
    LessThanOrEquals,
    GreaterThan,
    GreaterThanOrEquals,
}
