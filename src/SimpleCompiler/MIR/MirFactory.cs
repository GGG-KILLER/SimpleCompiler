using Loretta.CodeAnalysis;

namespace SimpleCompiler.MIR;

public static partial class MirFactory
{
    public static DiscardExpression DiscardExpression(SyntaxReference? reference) =>
        DiscardExpression(reference, ResultKind.None);
    public static ConstantExpression NilConstant(SyntaxReference? reference) =>
        ConstantExpression(reference, ConstantKind.Nil, null!);
    public static ConstantExpression TrueConstant(SyntaxReference? reference) =>
        ConstantExpression(reference, ConstantKind.Boolean, true);
    public static ConstantExpression FalseConstant(SyntaxReference? reference) =>
        ConstantExpression(reference, ConstantKind.Boolean, false);
    public static ConstantExpression IntegerConstant(SyntaxReference? reference, long value) =>
        ConstantExpression(reference, ResultKind.Int, ConstantKind.Number, value);
    public static ConstantExpression DoubleConstant(SyntaxReference? reference, double value) =>
        ConstantExpression(reference, ResultKind.Double, ConstantKind.Number, value);
    public static ConstantExpression StringConstant(SyntaxReference? reference, string value) =>
        ConstantExpression(reference, ConstantKind.String, value);

    public static ConstantExpression ConstantExpression(ConstantKind constantKind, object value) => ConstantExpression(null, constantKind, value);
    public static ConstantExpression ConstantExpression(SyntaxReference? reference, ConstantKind constantKind, object value) =>
        ConstantExpression(
            reference,
            constantKind switch
            {
                ConstantKind.Nil => ResultKind.Nil,
                ConstantKind.Boolean => ResultKind.Bool,
                ConstantKind.Number => ResultKind.Double,
                ConstantKind.String => ResultKind.Str,
                _ => throw new ArgumentException("Invalid constant kind.", nameof(constantKind))
            },
            constantKind,
            value);
}