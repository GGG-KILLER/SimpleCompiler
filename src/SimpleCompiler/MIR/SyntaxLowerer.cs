using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Syntax;
using SimpleCompiler.Helpers;

namespace SimpleCompiler.MIR;

/// <summary>
/// "Lowers" the higher level syntax into a lower level representation.
/// </summary>
public sealed class SyntaxLowerer : LuaSyntaxVisitor<MirNode>
{
    private readonly ScopeInfo _globalScope;
    private readonly ScopeInfo _fileScope;
    private readonly Stack<ScopeInfo> _scopes = new();

    public SyntaxLowerer(ScopeInfo globalScope)
    {
        _globalScope = globalScope;
        _scopes.Push(_globalScope);
        _fileScope = new ScopeInfo(ScopeKind.File, _globalScope);
        _scopes.Push(_fileScope);
    }

    private VariableInfo FindOrCreateVariable(string name, VariableKind kind, ScopeKind upTo = ScopeKind.Global)
    {
        return _scopes.Peek().FindVariable(name, upTo) ?? new VariableInfo(_globalScope, kind, name);
    }

    public override MirNode? VisitStatementList(StatementListSyntax node) => VisitStatementList(node, null);

    public MirNode? VisitStatementList(StatementListSyntax node, ScopeInfo? scope)
    {
        var statements = MirListBuilder<Statement>.Create();
        foreach (var statement in node.Statements)
        {
            var lowered = Visit(statement);
            if (lowered is StatementList statementList && statementList.ScopeInfo is null)
                statements.AddRange(statementList.Statements);
            else
                statements.Add((Statement)lowered!);
        }

        return MirFactory.StatementList(statements.ToList(), scope);
    }

    public override MirNode? VisitLocalVariableDeclarationStatement(LocalVariableDeclarationStatementSyntax node)
    {
        var names = node.Names.ToArray();
        var values = node.EqualsValues?.Values.ToArray() ?? [];

        var len = Math.Max(names.Length, values.Length);
        var assignees = new MirListBuilder<Expression>(len);
        var valueNodes = new MirListBuilder<Expression>(len);
        for (var idx = 0; idx < len; idx++)
        {
            var name = names.Length > idx ? names[idx] : null;
            var value = values.Length > idx ? (Expression)Visit(values[idx])! : MirConstants.Nil;

            valueNodes.Add(value);
            if (name is not null)
            {
                var variableInfo = new VariableInfo(_scopes.Peek(), VariableKind.Local, name.IdentifierName.Name);
                assignees.Add(MirFactory.VariableExpression(variableInfo));
            }
            else
            {
                assignees.Add(MirConstants.Discard);
            }
        }

        var assignment = MirFactory.AssignmentStatement(assignees.ToList(), valueNodes.ToList());

        foreach (var variable in assignment.Assignees.OfType<VariableExpression>())
            variable.VariableInfo.AddWrite(assignment);
        foreach (var variable in assignment.Values.OfType<VariableExpression>())
            variable.VariableInfo.AddRead(assignment);

        return assignment;
    }

    public override MirNode? VisitAssignmentStatement(AssignmentStatementSyntax node)
    {
        var assignees = node.Variables.ToArray();
        var values = node.EqualsValues?.Values.ToArray() ?? [];

        var len = Math.Max(assignees.Length, values.Length);
        var assigneeNodes = new MirListBuilder<Expression>(len);
        var valueNodes = new MirListBuilder<Expression>(len);
        for (var idx = 0; idx < len; idx++)
        {
            var assignee = assignees.Length > idx ? assignees[idx] : null;
            var value = values.Length > idx ? (Expression)Visit(values[idx])! : MirConstants.Nil;

            valueNodes.Add(value);
            if (assignee is not null)
            {
                var assigneeNode = (Expression)Visit(assignee)!;
                assigneeNodes.Add(assigneeNode);
            }
            else
            {
                assigneeNodes.Add(MirConstants.Discard);
            }
        }

        var assignment = MirFactory.AssignmentStatement(assigneeNodes.ToList(), valueNodes.ToList());

        foreach (var variable in assignment.Assignees.OfType<VariableExpression>())
            variable.VariableInfo.AddWrite(assignment);
        foreach (var variable in assignment.Values.OfType<VariableExpression>())
            variable.VariableInfo.AddRead(assignment);

        return assignment;
    }

    public override MirNode? VisitFunctionCallExpression(FunctionCallExpressionSyntax node)
    {
        var callee = (Expression)Visit(node.Expression)!;
        var arguments = node.Argument switch
        {
            StringFunctionArgumentSyntax stringArgument => new MirList<Expression>((Expression)Visit(stringArgument.Expression)!),
            TableConstructorFunctionArgumentSyntax tableCtor => new MirList<Expression>((Expression)Visit(tableCtor.TableConstructor)!),
            ExpressionListFunctionArgumentSyntax expressionList => new MirList<Expression>(expressionList.Expressions.Select(x => (Expression)Visit(x)!)),
            _ => throw ExceptionUtil.Unreachable
        };

        return MirFactory.FunctionCallExpression(callee, arguments);
    }

    public override MirNode? VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        var left = (Expression)Visit(node.Left)!;
        var right = (Expression)Visit(node.Right)!;
        var binOpKind = node.Kind() switch
        {
            SyntaxKind.AddExpression => BinaryOperationKind.Addition,
            SyntaxKind.SubtractExpression => BinaryOperationKind.Subtraction,
            SyntaxKind.MultiplyExpression => BinaryOperationKind.Multiplication,
            SyntaxKind.DivideExpression => BinaryOperationKind.Division,
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

        return MirFactory.BinaryOperationExpression(binOpKind, left, right);
    }

    public override MirNode? VisitCompilationUnit(CompilationUnitSyntax node)
    {
        return VisitStatementList(node.Statements, _fileScope);
    }

    public override MirNode? VisitDoStatement(DoStatementSyntax node)
    {
        var scope = new ScopeInfo(ScopeKind.Block, _scopes.Peek());
        _scopes.Push(scope);

        var statements = VisitStatementList(node.Body, scope);

        if (!ReferenceEquals(_scopes.Pop(), scope))
            throw ExceptionUtil.Unreachable;

        return statements;
    }

    public override MirNode? VisitEmptyStatement(EmptyStatementSyntax node) => MirFactory.EmptyStatement();

    public override MirNode? VisitExpressionStatement(ExpressionStatementSyntax node) =>
        MirFactory.ExpressionStatement((Expression)Visit(node.Expression)!);

    public override MirNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        var variableInfo = FindOrCreateVariable(node.Name, VariableKind.Global);
        return MirFactory.VariableExpression(variableInfo);
    }

    public override MirNode? VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        return node.Kind() switch
        {
            SyntaxKind.NilLiteralExpression => MirConstants.Nil,
            SyntaxKind.TrueLiteralExpression => MirConstants.True,
            SyntaxKind.FalseLiteralExpression => MirConstants.False,
            SyntaxKind.StringLiteralExpression => MirFactory.ConstantExpression(ConstantKind.String, (string)node.Token.Value!),
            SyntaxKind.NumericalLiteralExpression => MirFactory.ConstantExpression(ConstantKind.Number, (double)node.Token.Value!),
            SyntaxKind kind => throw new NotSupportedException($"Constants with kind {kind} is not supported."),
        };
    }

    public override MirNode? VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) => Visit(node.Expression);

    public override MirNode? VisitUnaryExpression(UnaryExpressionSyntax node)
    {
        var unopKind = node.Kind() switch
        {
            SyntaxKind.BitwiseNotExpression => UnaryOperationKind.BitwiseNegation,
            SyntaxKind.LengthExpression => UnaryOperationKind.LengthOf,
            SyntaxKind.LogicalNotExpression => UnaryOperationKind.LogicalNegation,
            SyntaxKind.UnaryMinusExpression => UnaryOperationKind.NumericalNegation,
            _ => throw ExceptionUtil.Unreachable,
        };
        var operand = (Expression)Visit(node.Operand)!;

        return MirFactory.UnaryOperationExpression(unopKind, operand);
    }

    public override MirNode? DefaultVisit(SyntaxNode node) =>
        throw new NotImplementedException($"Lowering for {node} hasn't been implemented yet.");
}
