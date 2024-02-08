using System.Collections.Frozen;

namespace SimpleCompiler.MIR.Ssa;

public sealed partial class SsaComputer(MirTree tree)
{
    private State? _state;
    public MirTree Tree { get; } = tree;

    private FrozenDictionary<MirNode, SsaBlock> BlocksByNode =>
        _state?.BlocksByNode ?? throw new InvalidOperationException("State hasn't been computed.");
    private FrozenDictionary<ScopeInfo, SsaBlock> BlocksByScope =>
        _state?.BlocksByScope ?? throw new InvalidOperationException("State hasn't been computed.");
    private FrozenDictionary<MirNode, SsaVariable> VariablesByNode =>
        _state?.VariablesByNode ?? throw new InvalidOperationException("State hasn't been computed.");
    private FrozenDictionary<VariableInfo, SsaVariable> VariablesByInfo =>
        _state?.VariablesByInfo ?? throw new InvalidOperationException("State hasn't been computed.");
    private FrozenDictionary<MirNode, SsaValueVersion> VersionsByNode =>
        _state?.VersionsByNode ?? throw new InvalidOperationException("State hasn't been computed.");

    public void Compute()
    {
        if (_state is null)
        {
            Interlocked.CompareExchange(ref _state, CalculateState(Tree), null);
        }
    }

    public SsaBlock? FindBlock(MirNode? node)
    {
        Compute();

        for (; node is not null; node = node.Parent)
        {
            if (BlocksByNode.TryGetValue(node, out var block))
                return block;
        }

        return null;
    }

    public SsaBlock? FindBlock(ScopeInfo scope)
    {
        Compute();

        return BlocksByScope.TryGetValue(scope, out var block) ? block : null;
    }

    public SsaVariable? GetVariable(MirNode node)
    {
        Compute();

        return VariablesByNode.TryGetValue(node, out var ssaVar) ? ssaVar : null;
    }

    public SsaVariable? GetVariable(VariableInfo variable)
    {
        Compute();

        return VariablesByInfo.TryGetValue(variable, out var ssaVar) ? ssaVar : null;
    }

    public SsaValueVersion? GetVariableVersion(VariableExpression variable)
    {
        Compute();

        if (VersionsByNode.TryGetValue(variable, out var value))
            return value;

        var ssaVar = GetVariable(variable);
        if (ssaVar is null)
            return null;

        return ssaVar.GetVersionAtPoint(variable);
    }
}
