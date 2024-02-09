using System.Runtime.CompilerServices;
using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Syntax;
using SimpleCompiler.Helpers;

namespace SimpleCompiler.IR;

public sealed class IrLifter : IrVisitor<SyntaxNode>
{
    private IrLifter()
    {
    }

    public static SyntaxNode Lift(IrNode node) => new IrLifter().Visit(node)!.NormalizeWhitespace();

    public override SyntaxNode? VisitStatementList(StatementList node)
    {
        var statements = SyntaxFactory.StatementList(
            SyntaxFactory.List<SyntaxNode>(node.Statements.Select(Visit).Where(x => x is not null)!)
        );

        if (node.ScopeInfo?.Kind == ScopeKind.Block)
        {
            return SyntaxFactory.DoStatement(statements);
        }
        else
        {
            return statements;
        }
    }

    public override SyntaxNode? VisitAssignmentStatement(AssignmentStatement node)
    {
        if (node.Assignees.All(x => isLocalVar(node, x)))
        {
            var names = new List<LocalDeclarationNameSyntax>();
            var values = new List<ExpressionSyntax>();

            for (var idx = 0; idx < node.Assignees.Count; idx++)
            {
                if (node.Assignees[idx] is not DiscardExpression)
                {
                    var variable = (VariableExpression) node.Assignees[idx];
                    names.Add(SyntaxFactory.LocalDeclarationName(variable.VariableInfo.Name));
                }
                values.Add((ExpressionSyntax) Visit(node.Values[idx])!);
            }

            return SyntaxFactory.LocalVariableDeclarationStatement(
                SyntaxFactory.SeparatedList(names),
                SyntaxFactory.SeparatedList(values)
            );
        }
        else
        {
            var assignees = new List<PrefixExpressionSyntax>();
            var values = new List<ExpressionSyntax>();

            for (var idx = 0; idx < node.Assignees.Count; idx++)
            {
                if (node.Assignees[idx] is not DiscardExpression)
                    assignees.Add((PrefixExpressionSyntax) Visit(node.Assignees[idx])!);
                values.Add((ExpressionSyntax) Visit(node.Values[idx])!);
            }

            return SyntaxFactory.AssignmentStatement(
                SyntaxFactory.SeparatedList(assignees),
                SyntaxFactory.SeparatedList(values)
            );
        }

        static bool isLocalVar(AssignmentStatement parent, Expression node) =>
            node is DiscardExpression
            || (node is VariableExpression var
                && var.VariableInfo.Kind == VariableKind.Local
                && var.VariableInfo.Writes.Count > 0
                && var.VariableInfo.Writes[0].IsEquivalentTo(parent));
    }

    public override SyntaxNode? VisitEmptyStatement(EmptyStatement node) => SyntaxFactory.EmptyStatement();

    public override SyntaxNode? VisitExpressionStatement(ExpressionStatement node) =>
        SyntaxFactory.ExpressionStatement((ExpressionSyntax) Visit(node.Expression)!);

    public override SyntaxNode? VisitBinaryOperationExpression(BinaryOperationExpression node)
    {
        var (opKind, tokenKind) = node.BinaryOperationKind switch
        {
            BinaryOperationKind.Addition => (SyntaxKind.AddExpression, SyntaxKind.PlusToken),
            BinaryOperationKind.Subtraction => (SyntaxKind.SubtractExpression, SyntaxKind.MinusToken),
            BinaryOperationKind.Multiplication => (SyntaxKind.MultiplyExpression, SyntaxKind.StarToken),
            BinaryOperationKind.Division => (SyntaxKind.DivideExpression, SyntaxKind.SlashToken),
            BinaryOperationKind.IntegerDivision => (SyntaxKind.FloorDivideExpression, SyntaxKind.SlashSlashToken),
            BinaryOperationKind.Modulo => (SyntaxKind.ModuloExpression, SyntaxKind.PercentToken),
            BinaryOperationKind.Exponentiation => (SyntaxKind.ExponentiateExpression, SyntaxKind.HatToken),
            BinaryOperationKind.Concatenation => (SyntaxKind.ConcatExpression, SyntaxKind.DotDotToken),

            BinaryOperationKind.BooleanAnd => (SyntaxKind.LogicalAndExpression, SyntaxKind.AndKeyword),
            BinaryOperationKind.BooleanOr => (SyntaxKind.LogicalOrExpression, SyntaxKind.OrKeyword),
            BinaryOperationKind.BitwiseAnd => (SyntaxKind.BitwiseAndExpression, SyntaxKind.AmpersandToken),
            BinaryOperationKind.BitwiseOr => (SyntaxKind.BitwiseOrExpression, SyntaxKind.PipeToken),
            BinaryOperationKind.BitwiseXor => (SyntaxKind.ExclusiveOrExpression, SyntaxKind.TildeToken),
            BinaryOperationKind.LeftShift => (SyntaxKind.LeftShiftExpression, SyntaxKind.LessThanLessThanToken),
            BinaryOperationKind.RightShift => (SyntaxKind.RightShiftExpression, SyntaxKind.GreaterThanGreaterThanToken),

            BinaryOperationKind.Equals => (SyntaxKind.EqualsExpression, SyntaxKind.EqualsEqualsToken),
            BinaryOperationKind.NotEquals => (SyntaxKind.NotEqualsExpression, SyntaxKind.TildeEqualsToken),
            BinaryOperationKind.LessThan => (SyntaxKind.LessThanExpression, SyntaxKind.LessThanToken),
            BinaryOperationKind.LessThanOrEquals => (SyntaxKind.LessThanOrEqualExpression, SyntaxKind.LessThanEqualsToken),
            BinaryOperationKind.GreaterThan => (SyntaxKind.GreaterThanExpression, SyntaxKind.GreaterThanToken),
            BinaryOperationKind.GreaterThanOrEquals => (SyntaxKind.GreaterThanOrEqualExpression, SyntaxKind.GreaterThanEqualsToken),

            _ => throw ExceptionUtil.Unreachable
        };

        return SyntaxFactory.BinaryExpression(
            opKind,
            (ExpressionSyntax) Visit(node.Left)!,
            SyntaxFactory.Token(tokenKind),
            (ExpressionSyntax) Visit(node.Right)!
        );
    }

    public override SyntaxNode? VisitUnaryOperationExpression(UnaryOperationExpression node)
    {
        var (opKind, tokenKind) = node.UnaryOperationKind switch
        {
            UnaryOperationKind.BitwiseNegation => (SyntaxKind.BitwiseNotExpression, SyntaxKind.TildeToken),
            UnaryOperationKind.LengthOf => (SyntaxKind.LengthExpression, SyntaxKind.HashToken),
            UnaryOperationKind.LogicalNegation => (SyntaxKind.LogicalNotExpression, SyntaxKind.NotKeyword),
            UnaryOperationKind.NumericalNegation => (SyntaxKind.UnaryMinusExpression, SyntaxKind.MinusToken),

            _ => throw ExceptionUtil.Unreachable
        };

        return SyntaxFactory.UnaryExpression(
            opKind,
            SyntaxFactory.Token(tokenKind),
            (ExpressionSyntax) Visit(node.Operand)!
        );
    }

    public override SyntaxNode? VisitFunctionCallExpression(FunctionCallExpression node)
    {
        return SyntaxFactory.FunctionCallExpression(
            (PrefixExpressionSyntax) Visit(node.Callee)!,
            SyntaxFactory.ExpressionListFunctionArgument(
                SyntaxFactory.SeparatedList(
                    node.Arguments.Select(Visit).Where(x => x is not null).Select(x => (ExpressionSyntax) x!)
                )
            )
        );
    }

    public override SyntaxNode? VisitConstantExpression(ConstantExpression node)
    {
        return node.ConstantKind switch
        {
            ConstantKind.Nil => SyntaxFactory.LiteralExpression(SyntaxKind.NilLiteralExpression),
            ConstantKind.Boolean => node.Value is true
                ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                : SyntaxFactory.LiteralExpression(SyntaxKind.FalseKeyword),
            ConstantKind.Number => SyntaxFactory.LiteralExpression(SyntaxKind.NumericalLiteralExpression, node.Value is long i64
                ? SyntaxFactory.Literal(i64)
                : SyntaxFactory.Literal(Unsafe.Unbox<double>(node.Value!))),
            ConstantKind.String => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(Unsafe.As<string>(node.Value)!)),

            _ => throw ExceptionUtil.Unreachable
        };
    }

    public override SyntaxNode? VisitDiscardExpression(DiscardExpression node) =>
        throw new InvalidOperationException("Cannot lift a discard expression.");

    public override SyntaxNode? VisitVariableExpression(VariableExpression node) =>
        SyntaxFactory.IdentifierName(node.VariableInfo.Name);

    protected override SyntaxNode? DefaultVisit(IrNode node) =>
        throw new NotImplementedException($"Lifting not implemented for {node.GetType()}");
}
