using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Syntax;
using SimpleCompiler.Helpers;

namespace SimpleCompiler.Frontends.Lua;

using SimpleCompiler.IR;

/// <summary>
/// "Lowers" the higher level syntax into a lower level representation.
/// </summary>
public sealed class SyntaxLowerer : LuaSyntaxVisitor<IrNode>
{
    private readonly ScopeInfo _globalScope;
    private readonly ScopeInfo _fileScope;
    private readonly Stack<ScopeInfo> _scopes = new();

    public SyntaxLowerer(ScopeInfo globalScope)
    {
        _globalScope = globalScope;
        _scopes.Push(_globalScope);
        _fileScope = new ScopeInfo(ScopeKind.File, _globalScope);
        _globalScope.AddChildScope(_fileScope);
        CreateVariable(_fileScope, "args", VariableKind.Parameter);
        CreateVariable(_fileScope, "...", VariableKind.Parameter);
        _scopes.Push(_fileScope);
    }

    private static VariableInfo CreateVariable(ScopeInfo scope, string name, VariableKind kind)
    {
        var var = new VariableInfo(scope, kind, name);
        scope.AddDeclaredVariable(var);
        return var;
    }

    private VariableInfo FindOrCreateVariable(string name, VariableKind kind, ScopeKind upTo = ScopeKind.Global) =>
        _scopes.Peek().FindVariable(name, upTo) ?? CreateVariable(_globalScope, name, kind);

    public override IrNode? VisitStatementList(StatementListSyntax node) => VisitStatementList(node, null);

    public IrNode? VisitStatementList(StatementListSyntax node, ScopeInfo? scope)
    {
        var statements = new IrListBuilder<Statement>(node.Statements.Count);
        foreach (var statement in node.Statements)
        {
            var lowered = Visit(statement);
            if (lowered is StatementList statementList && statementList.ScopeInfo is null)
                statements.AddRange(statementList.Statements);
            else
                statements.Add((Statement) lowered!);
        }

        return IrFactory.StatementList(node.GetReference(), statements.ToList(), scope);
    }

    public override IrNode? VisitLocalVariableDeclarationStatement(LocalVariableDeclarationStatementSyntax node)
    {
        var syntaxNames = node.Names.ToArray();
        var syntaxValues = node.EqualsValues?.Values.ToArray() ?? [];

        var len = Math.Max(syntaxNames.Length, syntaxValues.Length);
        var assignees = new IrListBuilder<Expression>(len);
        var values = new IrListBuilder<Expression>(len);
        for (var idx = 0; idx < len; idx++)
        {
            var nameSyntax = syntaxNames.Length > idx ? syntaxNames[idx] : null;
            var valueSyntax = syntaxValues.Length > idx ? syntaxValues[idx] : null;
            var value = valueSyntax is not null ? (Expression) Visit(valueSyntax)! : IrFactory.NilConstant(null);

            values.Add(value);
            if (nameSyntax is not null)
            {
                var variableInfo = CreateVariable(_scopes.Peek(), nameSyntax.IdentifierName.Name, VariableKind.Local);
                assignees.Add(IrFactory.VariableExpression(nameSyntax.GetReference(), ResultKind.Any, variableInfo));
            }
            else
            {
                assignees.Add(IrFactory.DiscardExpression(null));
            }
        }

        var assignment = IrFactory.AssignmentStatement(node.GetReference(), assignees.ToList(), values.ToList());

        foreach (var variable in assignment.Assignees.OfType<VariableExpression>())
            variable.VariableInfo.AddWrite(assignment);

        return assignment;
    }

    public override IrNode? VisitAssignmentStatement(AssignmentStatementSyntax node)
    {
        var assignees = node.Variables.ToArray();
        var values = node.EqualsValues?.Values.ToArray() ?? [];

        var len = Math.Max(assignees.Length, values.Length);
        var assigneeNodes = new IrListBuilder<Expression>(len);
        var valueNodes = new IrListBuilder<Expression>(len);
        for (var idx = 0; idx < len; idx++)
        {
            var assignee = assignees.Length > idx ? assignees[idx] : null;
            var value = values.Length > idx ? (Expression) Visit(values[idx])! : IrFactory.NilConstant(null);

            valueNodes.Add(value);
            if (assignee is not null)
            {
                var assigneeNode = (Expression) Visit(assignee)!;
                assigneeNodes.Add(assigneeNode);
            }
            else
            {
                assigneeNodes.Add(IrFactory.DiscardExpression(null));
            }
        }

        var assignment = IrFactory.AssignmentStatement(node.GetReference(), assigneeNodes.ToList(), valueNodes.ToList());

        foreach (var variable in assignment.Assignees.OfType<VariableExpression>())
            variable.VariableInfo.AddWrite(assignment);

        return assignment;
    }

    public override IrNode? VisitFunctionCallExpression(FunctionCallExpressionSyntax node)
    {
        var callee = (Expression) Visit(node.Expression)!;
        var arguments = node.Argument switch
        {
            StringFunctionArgumentSyntax stringArgument => new IrList<Expression>((Expression) Visit(stringArgument.Expression)!),
            TableConstructorFunctionArgumentSyntax tableCtor => new IrList<Expression>((Expression) Visit(tableCtor.TableConstructor)!),
            ExpressionListFunctionArgumentSyntax expressionList => new IrList<Expression>(expressionList.Expressions.Select(x => (Expression) Visit(x)!)),
            _ => throw ExceptionUtil.Unreachable
        };

        return IrFactory.FunctionCallExpression(node.GetReference(), ResultKind.Any, callee, arguments);
    }

    public override IrNode? VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        var left = (Expression) Visit(node.Left)!;
        var right = (Expression) Visit(node.Right)!;
        var opKind = node.Kind() switch
        {
            SyntaxKind.AddExpression => BinaryOperationKind.Addition,
            SyntaxKind.SubtractExpression => BinaryOperationKind.Subtraction,
            SyntaxKind.MultiplyExpression => BinaryOperationKind.Multiplication,
            SyntaxKind.DivideExpression => BinaryOperationKind.Division,
            SyntaxKind.FloorDivideExpression => BinaryOperationKind.IntegerDivision,
            SyntaxKind.ModuloExpression => BinaryOperationKind.Modulo,
            SyntaxKind.ExponentiateExpression => BinaryOperationKind.Exponentiation,
            SyntaxKind.ConcatExpression => BinaryOperationKind.Concatenation,

            SyntaxKind.LogicalAndExpression => BinaryOperationKind.BooleanAnd,
            SyntaxKind.LogicalOrExpression => BinaryOperationKind.BooleanOr,
            SyntaxKind.BitwiseAndExpression => BinaryOperationKind.BitwiseAnd,
            SyntaxKind.BitwiseOrExpression => BinaryOperationKind.BitwiseOr,
            SyntaxKind.ExclusiveOrExpression => BinaryOperationKind.BitwiseXor,
            SyntaxKind.LeftShiftExpression => BinaryOperationKind.LeftShift,
            SyntaxKind.RightShiftExpression => BinaryOperationKind.RightShift,

            SyntaxKind.EqualsExpression => BinaryOperationKind.Equals,
            SyntaxKind.NotEqualsExpression => BinaryOperationKind.NotEquals,
            SyntaxKind.LessThanExpression => BinaryOperationKind.LessThan,
            SyntaxKind.LessThanOrEqualExpression => BinaryOperationKind.LessThanOrEquals,
            SyntaxKind.GreaterThanExpression => BinaryOperationKind.GreaterThan,
            SyntaxKind.GreaterThanOrEqualExpression => BinaryOperationKind.GreaterThanOrEquals,

            _ => throw ExceptionUtil.Unreachable,
        };

        var resKind = opKind switch
        {
            BinaryOperationKind.Addition
            or BinaryOperationKind.Subtraction
            or BinaryOperationKind.Multiplication
            or BinaryOperationKind.Division
            or BinaryOperationKind.Modulo => left.ResultKind is ResultKind.Int && right.ResultKind is ResultKind.Int ? ResultKind.Int : ResultKind.Double,

            BinaryOperationKind.Exponentiation => ResultKind.Double,

            BinaryOperationKind.IntegerDivision
            or BinaryOperationKind.BitwiseAnd
            or BinaryOperationKind.BitwiseOr
            or BinaryOperationKind.BitwiseXor
            or BinaryOperationKind.LeftShift
            or BinaryOperationKind.RightShift => ResultKind.Int,

            BinaryOperationKind.Concatenation => ResultKind.Str,

            BinaryOperationKind.BooleanAnd => left.ResultKind | right.ResultKind,
            BinaryOperationKind.BooleanOr => left.ResultKind | right.ResultKind,

            BinaryOperationKind.Equals
            or BinaryOperationKind.NotEquals
            or BinaryOperationKind.LessThan
            or BinaryOperationKind.LessThanOrEquals
            or BinaryOperationKind.GreaterThan
            or BinaryOperationKind.GreaterThanOrEquals => ResultKind.Bool,

            _ => ResultKind.Any
        };

        return IrFactory.BinaryOperationExpression(node.GetReference(), resKind, opKind, left, right);
    }

    public override IrNode? VisitCompilationUnit(CompilationUnitSyntax node)
    {
        return VisitStatementList(node.Statements, _fileScope);
    }

    public override IrNode? VisitDoStatement(DoStatementSyntax node)
    {
        var scope = new ScopeInfo(ScopeKind.Block, _scopes.Peek());
        _scopes.Push(scope);

        var statements = VisitStatementList(node.Body, scope);

        if (!ReferenceEquals(_scopes.Pop(), scope))
            throw ExceptionUtil.Unreachable;

        return statements;
    }

    public override IrNode? VisitEmptyStatement(EmptyStatementSyntax node) => IrFactory.EmptyStatement(node.GetReference());

    public override IrNode? VisitExpressionStatement(ExpressionStatementSyntax node) =>
        IrFactory.ExpressionStatement(node.GetReference(), (Expression) Visit(node.Expression)!);

    public override IrNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        var variableInfo = FindOrCreateVariable(node.Name, VariableKind.Global);
        var mirNode = IrFactory.VariableExpression(node.GetReference(), ResultKind.Any, variableInfo);
        variableInfo.AddRead(mirNode);
        return mirNode;
    }

    public override IrNode? VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        return node.Kind() switch
        {
            SyntaxKind.NilLiteralExpression => IrFactory.NilConstant(node.GetReference()),
            SyntaxKind.TrueLiteralExpression => IrFactory.TrueConstant(node.GetReference()),
            SyntaxKind.FalseLiteralExpression => IrFactory.FalseConstant(node.GetReference()),
            SyntaxKind.StringLiteralExpression => IrFactory.ConstantExpression(node.GetReference(), ResultKind.Str, ConstantKind.String, (string) node.Token.Value!),
            SyntaxKind.NumericalLiteralExpression => IrFactory.ConstantExpression(node.GetReference(), ResultKind.Double, ConstantKind.Number, (double) node.Token.Value!),
            SyntaxKind kind => throw new NotSupportedException($"Constants with kind {kind} is not supported."),
        };
    }

    public override IrNode? VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) => Visit(node.Expression);

    public override IrNode? VisitUnaryExpression(UnaryExpressionSyntax node)
    {
        var operand = (Expression) Visit(node.Operand)!;

        var resKind = node.Kind() switch
        {
            SyntaxKind.BitwiseNotExpression => ResultKind.Int,
            SyntaxKind.LengthExpression => ResultKind.Int,
            SyntaxKind.LogicalNotExpression => ResultKind.Bool,
            SyntaxKind.UnaryMinusExpression => operand.ResultKind == ResultKind.Int ? ResultKind.Int : ResultKind.Double,
            _ => ResultKind.Any
        };

        var opKind = node.Kind() switch
        {
            SyntaxKind.BitwiseNotExpression => UnaryOperationKind.BitwiseNegation,
            SyntaxKind.LengthExpression => UnaryOperationKind.LengthOf,
            SyntaxKind.LogicalNotExpression => UnaryOperationKind.LogicalNegation,
            SyntaxKind.UnaryMinusExpression => UnaryOperationKind.NumericalNegation,
            _ => throw ExceptionUtil.Unreachable,
        };

        return IrFactory.UnaryOperationExpression(node.GetReference(), resKind, opKind, operand);
    }

    public override IrNode? DefaultVisit(SyntaxNode node) =>
        throw new NotImplementedException($"Lowering for {node} hasn't been implemented yet.");
}
