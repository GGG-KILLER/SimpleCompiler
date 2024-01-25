namespace SimpleCompiler.MIR;

public enum BinaryOperationKind
{
    Addition,
    Subtraction,
    Multiplication,
    Division,
    IntegerDivision,
    Modulo,
    Concatenation,
    Exponentiation,

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
