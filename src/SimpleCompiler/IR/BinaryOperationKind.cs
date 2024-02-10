namespace SimpleCompiler.IR;

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

public static class BinaryOperationKindExtensions
{
    public static bool IsArithmetic(this BinaryOperationKind kind) =>
        kind is BinaryOperationKind.Addition or BinaryOperationKind.Subtraction
             or BinaryOperationKind.Multiplication or BinaryOperationKind.Division
             or BinaryOperationKind.Exponentiation or BinaryOperationKind.Modulo
             or BinaryOperationKind.IntegerDivision;

    public static bool IsBitwise(this BinaryOperationKind kind) =>
        kind is BinaryOperationKind.BitwiseAnd or BinaryOperationKind.BitwiseOr
             or BinaryOperationKind.LeftShift or BinaryOperationKind.RightShift
             or BinaryOperationKind.BitwiseXor;

    public static bool IsNumericComparison(this BinaryOperationKind kind) =>
        kind is BinaryOperationKind.LessThan or BinaryOperationKind.LessThanOrEquals
             or BinaryOperationKind.GreaterThan or BinaryOperationKind.GreaterThanOrEquals;

    public static bool IsComparison(this BinaryOperationKind kind) =>
        kind is BinaryOperationKind.Equals or BinaryOperationKind.NotEquals
             or BinaryOperationKind.LessThan or BinaryOperationKind.LessThanOrEquals
             or BinaryOperationKind.GreaterThan or BinaryOperationKind.GreaterThanOrEquals;
}
