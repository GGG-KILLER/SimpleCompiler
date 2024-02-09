namespace SimpleCompiler.IR;

public sealed class VariableInfo(ScopeInfo scope, VariableKind kind, string name)
{
    private readonly List<IrNode> _reads = [], _writes = [];

    public VariableKind Kind { get; } = kind;
    public ScopeInfo Scope { get; } = scope;
    public string Name { get; } = name;
    public IReadOnlyList<IrNode> Reads => _reads;
    public IReadOnlyList<IrNode> Writes => _writes;

    internal void AddRead(IrNode node) => _reads.Add(node);
    internal void AddWrite(IrNode node) => _writes.Add(node);
}
