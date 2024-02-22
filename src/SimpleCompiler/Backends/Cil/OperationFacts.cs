using SimpleCompiler.IR;

namespace SimpleCompiler.Backends.Cil;

internal static class OperationFacts
{
    public static SymbolType GetOperationOutput(UnaryOperationKind kind, SymbolType input) =>
        kind switch
        {
            UnaryOperationKind.LogicalNegation => SymbolType.Boolean,
            UnaryOperationKind.BitwiseNegation => SymbolType.Long,
            UnaryOperationKind.NumericalNegation => input == SymbolType.Long ? SymbolType.Long : SymbolType.Double,
            UnaryOperationKind.LengthOf => SymbolType.Long,
            _ => throw new NotImplementedException($"{nameof(GetOperationOutput)} not implemented for unary kind {kind}.")
        };

    public static SymbolType GetOperationOutput(BinaryOperationKind kind, SymbolType left, SymbolType right) =>
        kind switch
        {
            BinaryOperationKind.Addition => left == SymbolType.Long && right == SymbolType.Long ? SymbolType.Long : SymbolType.Double,
            BinaryOperationKind.Subtraction => left == SymbolType.Long && right == SymbolType.Long ? SymbolType.Long : SymbolType.Double,
            BinaryOperationKind.Multiplication => left == SymbolType.Long && right == SymbolType.Long ? SymbolType.Long : SymbolType.Double,
            BinaryOperationKind.Division => SymbolType.Double,
            BinaryOperationKind.IntegerDivision => SymbolType.Long,
            BinaryOperationKind.Exponentiation => SymbolType.Double,
            BinaryOperationKind.Modulo => left == SymbolType.Long && right == SymbolType.Long ? SymbolType.Long : SymbolType.Double,
            BinaryOperationKind.Concatenation => SymbolType.String,
            BinaryOperationKind.BitwiseAnd => SymbolType.Long,
            BinaryOperationKind.BitwiseOr => SymbolType.Long,
            BinaryOperationKind.BitwiseXor => SymbolType.Long,
            BinaryOperationKind.LeftShift => SymbolType.Long,
            BinaryOperationKind.RightShift => SymbolType.Long,
            BinaryOperationKind.Equals => SymbolType.Boolean,
            BinaryOperationKind.NotEquals => SymbolType.Boolean,
            BinaryOperationKind.LessThan => SymbolType.Boolean,
            BinaryOperationKind.LessThanOrEquals => SymbolType.Boolean,
            BinaryOperationKind.GreaterThan => SymbolType.Boolean,
            BinaryOperationKind.GreaterThanOrEquals => SymbolType.Boolean,
            _ => throw new NotImplementedException($"{nameof(GetOperationOutput)} not implemented for binary kind {kind}.")
        };
}
