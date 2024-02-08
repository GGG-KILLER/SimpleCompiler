using System.Diagnostics;

namespace SimpleCompiler.MIR;

internal sealed class ScopeRemapper : MirRewriter
{
    private readonly ScopeInfo _globalScope;
    private readonly ScopeInfo _fileScope;
    private readonly Stack<ScopeInfo> _scopes = new();

    public ScopeRemapper(ScopeInfo globalScope)
    {
        _globalScope = globalScope;
        _scopes.Push(_globalScope);
        _fileScope = new ScopeInfo(ScopeKind.File, _globalScope);
        _globalScope.AddChildScope(_fileScope);
        CreateVariable(_fileScope, "args", VariableKind.Parameter);
        CreateVariable(_fileScope, "...", VariableKind.Parameter);
        _scopes.Push(_fileScope);
    }

    private ScopeInfo? FindScope(ScopeKind kind)
    {
        foreach (var scope in _scopes)
        {
            if (scope.Kind == kind)
                return scope;
        }
        return null;
    }

    private static VariableInfo CreateVariable(ScopeInfo scope, string name, VariableKind kind)
    {
        var var = new VariableInfo(scope, kind, name);
        scope.AddDeclaredVariable(var);
        return var;
    }

    private VariableInfo FindOrCreateVariable(string name, VariableKind kind, ScopeKind upTo = ScopeKind.Global)
    {
        if (_scopes.Peek().FindVariable(name, upTo) is { } local)
            return local;

        return CreateVariable(kind switch
        {
            VariableKind.Iteration => FindScope(ScopeKind.Loop),
            VariableKind.Parameter => FindScope(ScopeKind.Function),
            VariableKind.Local => _scopes.Peek(),
            VariableKind.Global => _globalScope,
            _ => null,
        } ?? _globalScope, name, kind);
    }

    public override MirNode VisitStatementList(StatementList node)
    {
        ScopeInfo? scope = null;
        if (node.ScopeInfo is not null)
        {
            var parentScope = _scopes.Peek();
            scope = new ScopeInfo(node.ScopeInfo.Kind, parentScope);
            parentScope.AddChildScope(scope);
            _scopes.Push(scope);
        }

        var statements = VisitList(node.Statements);
        node = node.Update(node.OriginalNode, statements, scope);

        if (scope is not null && !ReferenceEquals(scope, _scopes.Pop()))
            throw new UnreachableException("Popped scope is not the same as the inserted one.");

        return node;
    }

    public override MirNode VisitAssignmentStatement(AssignmentStatement node)
    {
        var assignees = new MirListBuilder<Expression>(node.Assignees.Count);
        var values = new MirListBuilder<Expression>(node.Values.Count);
        for (var idx = 0; idx < node.Assignees.Count; idx++)
        {
            if (node.Assignees[idx] is VariableExpression variableExpression)
            {
                assignees.Add(variableExpression.Update(
                    variableExpression.OriginalNode,
                    variableExpression.ResultKind,
                    FindOrCreateVariable(variableExpression.VariableInfo.Name, variableExpression.VariableInfo.Kind)));
            }
            else
            {
                assignees.Add((Expression) Visit(node.Assignees[idx])!);
            }

            values.Add((Expression) Visit(node.Values[idx])!);
        }

        node = node.Update(node.OriginalNode, assignees.ToList(), values.ToList());
        foreach (var var in node.Assignees.OfType<VariableExpression>())
        {
            var.VariableInfo.AddWrite(node);
        }

        return node;
    }

    public override MirNode VisitVariableExpression(VariableExpression node)
    {
        var variable = FindOrCreateVariable(node.VariableInfo.Name, node.VariableInfo.Kind);
        node = node.WithVariableInfo(variable);
        variable.AddRead(node);
        return node;
    }
}
