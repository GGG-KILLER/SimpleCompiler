using SimpleCompiler.MIR;

namespace SimpleCompiler;

public enum VariableKind
{
    Iteration,
    Local,
    Parameter,
    Global
}

public sealed class VariableInfo
{
    private readonly List<MirNode> _reads = [], _writes = [];

    public VariableKind Kind { get; }
    public ScopeInfo Scope { get; }
    public string Name { get; }
    public IReadOnlyList<MirNode> Reads => _reads;
    public IReadOnlyList<MirNode> Writes => _writes;

    public VariableInfo(ScopeInfo scope, VariableKind kind, string name)
    {
        scope.AddDeclaredVariable(this);
        Scope = scope;
        Kind = kind;
        Name = name;
    }

    internal void AddRead(MirNode node) => _reads.Add(node);
    internal void AddWrite(MirNode node) => _writes.Add(node);
}
