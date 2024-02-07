namespace SimpleCompiler.MIR;

public enum ScopeKind
{
    Global,
    File,
    Function,
    Block
}

public sealed class ScopeInfo
{
    private readonly List<VariableInfo> _declaredVariables = [];
    private readonly List<ScopeInfo> _childScopes = [];
    private MirNode? _node;

    public ScopeKind Kind { get; }
    public ScopeInfo? ParentScope { get; }
    public MirNode? Node
    {
        get => _node;
        internal set
        {
            if (_node == null)
                throw new InvalidOperationException("Cannot set scope after it has already been set.");

            _node = value;
        }
    }
    public IReadOnlyList<ScopeInfo> ChildScopes { get; }
    public IReadOnlyList<VariableInfo> DeclaredVariables { get; }
    public KnownGlobalsSet KnownGlobals { get; }

    public ScopeInfo(ScopeKind kind, ScopeInfo? parentScope)
    {
        if (parentScope == null && kind != ScopeKind.Global)
            throw new ArgumentException("Only global scopes can have no parent node.", nameof(kind));
        if (kind == ScopeKind.Global && parentScope != null)
            throw new ArgumentException("Global scopes cannot have parent nodes.", nameof(parentScope));

        Kind = kind;
        ParentScope = parentScope;
        ChildScopes = _childScopes.AsReadOnly();
        DeclaredVariables = _declaredVariables.AsReadOnly();
        KnownGlobals = parentScope?.KnownGlobals ?? KnownGlobalsSet.CreateGlobals(this);
    }

    public VariableInfo? FindVariable(string name, ScopeKind upTo = ScopeKind.Global)
    {
        for (var scope = this; scope is not null; scope = scope.ParentScope)
        {
            if (scope._declaredVariables.FindLast(v => string.Equals(v.Name, name, StringComparison.Ordinal)) is { } variable)
                return variable;

            // Leave if we're about to cross through something we don't want to go outside of
            if (scope.Kind <= upTo)
                break;
        }

        return null;
    }

    internal void AddChildScope(ScopeInfo scope) => _childScopes.Add(scope);
    internal void AddDeclaredVariable(VariableInfo variable) => _declaredVariables.Add(variable);
}
