using System.Collections.Immutable;
using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Syntax;
using SimpleCompiler.MIR;

namespace SimpleCompiler;

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
        var statements = ImmutableArray.CreateBuilder<Statement>(node.Statements.Count);
        foreach (var statement in node.Statements)
        {
            var lowered = Visit(statement);
            if (lowered is StatementList statementList && statementList.ScopeInfo is null)
                statements.AddRange(statementList.Statements.Select(n => (Statement)n.WithParent(null)));
            else
                statements.Add((Statement)lowered!);
        }
        return new StatementList(statements.DrainToImmutable(), scope);
    }

    public override MirNode? VisitLocalVariableDeclarationStatement(LocalVariableDeclarationStatementSyntax node)
    {
        var names = node.Names.ToArray();
        var values = node.EqualsValues?.Values.ToArray() ?? [];

        var len = Math.Max(names.Length, values.Length);
        var statements = ImmutableArray.CreateBuilder<Statement>(len);
        for (var idx = 0; idx < len; idx++)
        {
            var name = names.Length > idx ? names[idx] : null;
            var value = values.Length > idx ? values[idx] : null;

            if (name is not null)
            {
                var variableInfo = new VariableInfo(_scopes.Peek(), VariableKind.Local, name.IdentifierName.Name);
                var variable = new Variable(variableInfo);

                Assignment assignment;
                if (value is not null)
                    assignment = new Assignment(variable, (Expression)Visit(value)!);
                else
                    assignment = new Assignment(variable, Constant.Nil);

                variableInfo.AddWrite(assignment);
                statements.Add(assignment);
            }
            else
            {
                statements.Add(new ExpressionStatement((Expression)Visit(value)!));
            }
        }

        return new StatementList(statements.MoveToImmutable(), null);
    }

    public override MirNode? VisitFunctionCallExpression(FunctionCallExpressionSyntax node)
    {
        var callee = (Expression)Visit(node.Expression)!;
        var arguments = node.Argument switch
        {
            StringFunctionArgumentSyntax stringArgument => ImmutableArray.Create((Expression)Visit(stringArgument.Expression)!),
            TableConstructorFunctionArgumentSyntax tableCtor => [(Expression)Visit(tableCtor.TableConstructor)!],
            ExpressionListFunctionArgumentSyntax expressionList => [.. expressionList.Expressions.Select(x => (Expression)Visit(x)!)],
            _ => throw ExceptionUtil.Unreachable
        };

        return new FunctionCall(callee, arguments);
    }

    public override MirNode? VisitAssignmentStatement(AssignmentStatementSyntax node)
    {
        var assignees = node.Variables.Select(x => (Expression)Visit(x)!).ToImmutableArray();
        var values = node.EqualsValues.Values.Select(x => (Expression)Visit(x)!).ToImmutableArray();

        var len = Math.Max(assignees.Length, values.Length);
        var statements = ImmutableArray.CreateBuilder<Statement>(len);
        for (var idx = 0; idx < len; idx++)
        {
            var assignee = assignees.Length > idx ? assignees[idx] : null;
            var value = values.Length > idx ? values[idx] : null;

            if (assignee is not null)
            {
                statements.Add(new Assignment(assignee, value is not null ? value : Constant.Nil));
            }
            else
            {
                if (value is null)
                    throw ExceptionUtil.Unreachable;
                statements.Add(new ExpressionStatement(value));
            }
        }

        return new StatementList(statements.MoveToImmutable(), null);
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

        return new BinaryOperation(binOpKind, left, right);
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

    public override MirNode? VisitEmptyStatement(EmptyStatementSyntax node) => new EmptyStatement();

    public override MirNode? VisitExpressionStatement(ExpressionStatementSyntax node) =>
        new ExpressionStatement((Expression)Visit(node.Expression)!);

    public override MirNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        var variableInfo = FindOrCreateVariable(node.Name, VariableKind.Global);
        return new Variable(variableInfo);
    }

    public override MirNode? VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        return node.Kind() switch
        {
            SyntaxKind.NilLiteralExpression => Constant.Nil,
            SyntaxKind.TrueLiteralExpression => Constant.True,
            SyntaxKind.FalseLiteralExpression => Constant.False,
            SyntaxKind.StringLiteralExpression => new Constant(ConstantKind.String, (string)node.Token.Value!),
            SyntaxKind.NumericalLiteralExpression => new Constant(ConstantKind.Number, (double)node.Token.Value!),
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

        return new UnaryOperation(unopKind, operand);
    }

    public override MirNode? DefaultVisit(SyntaxNode node) =>
        throw new NotImplementedException($"Lowering for {node} hasn't been implemented yet.");
}
