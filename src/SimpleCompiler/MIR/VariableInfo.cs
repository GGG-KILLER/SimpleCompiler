namespace SimpleCompiler.MIR;

public sealed class VariableInfo(ScopeInfo scope, VariableKind kind, string name)
{
    private readonly List<MirNode> _reads = [], _writes = [];

    public VariableKind Kind { get; } = kind;
    public ScopeInfo Scope { get; } = scope;
    public string Name { get; } = name;
    public IReadOnlyList<MirNode> Reads => _reads;
    public IReadOnlyList<MirNode> Writes => _writes;

    internal void AddRead(MirNode node) => _reads.Add(node);
    internal void AddWrite(MirNode node) => _writes.Add(node);
}
