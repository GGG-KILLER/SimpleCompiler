namespace SimpleCompiler.MIR;

public static partial class MirConstants
{
    public static readonly ConstantExpression Nil = MirFactory.ConstantExpression(ConstantKind.Nil, null!);
    public static readonly ConstantExpression True = MirFactory.ConstantExpression(ConstantKind.Boolean, true);
    public static readonly ConstantExpression False = MirFactory.ConstantExpression(ConstantKind.Boolean, false);
    public static readonly DiscardExpression Discard = MirFactory.DiscardExpression();

    public static readonly EmptyStatement EmptyStatement = MirFactory.EmptyStatement();
}