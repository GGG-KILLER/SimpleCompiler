namespace SimpleCompiler.IR;

public static class OperationFacts
{
    public static ResultKind DesiredOperand(UnaryOperationExpression expression)
    {
        return expression.UnaryOperationKind switch
        {
            UnaryOperationKind.LogicalNegation => ResultKind.Any,
            UnaryOperationKind.BitwiseNegation => ResultKind.Int,
            UnaryOperationKind.NumericalNegation => expression.Operand.ResultKind == ResultKind.Int ? ResultKind.Int : ResultKind.Double,
            _ => ResultKind.Any,
        };
    }

    public static (ResultKind left, ResultKind right) DesiredOperands(BinaryOperationExpression expression)
    {
        if (expression.BinaryOperationKind.IsArithmetic() || expression.BinaryOperationKind.IsNumericComparison())
            return expression.ResultKind == ResultKind.Int ? (ResultKind.Int, ResultKind.Int) : (ResultKind.Double, ResultKind.Double);
        else if (expression.BinaryOperationKind.IsBitwise())
            return (ResultKind.Int, ResultKind.Int);
        else
            return (ResultKind.Any, ResultKind.Any);
    }
}
