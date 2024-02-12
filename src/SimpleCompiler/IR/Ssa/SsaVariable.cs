namespace SimpleCompiler.IR.Ssa;

public sealed class SsaVariable
{
    private readonly List<SsaScope> _referencedBlocks = [];
    private readonly List<SsaValueVersion> _valueVersions = [];

    internal SsaVariable(VariableInfo variable, SsaScope block)
    {
        Variable = variable;
        DeclaredBlock = block;
    }

    public VariableInfo Variable { get; }
    public SsaScope DeclaredBlock { get; }
    public IReadOnlyList<SsaScope> ReferencedBlocks => _referencedBlocks;
    public IReadOnlyList<SsaValueVersion> ValueVersions => _valueVersions;

    internal void AddValueVersion(IrNode write, Expression value) =>
        _valueVersions.Add(new SsaValueVersion(_valueVersions.Count + 1, this, write, value));

    internal void AddPhiVersion(IrNode write, IEnumerable<Expression> values) =>
        _valueVersions.Add(new SsaValueVersion(_valueVersions.Count + 1, this, write, values));

    internal void AddReferencedBlock(SsaScope block)
    {
        if (DeclaredBlock == block)
            return;

        _referencedBlocks.Add(block);
    }

    public SsaValueVersion? GetVersionAtPoint(IrNode location)
    {
        for (var i = _valueVersions.Count - 1; i >= 0; i--)
        {
            var version = _valueVersions[i];
            if (version.WriteLocation.IsBeforeInPrefixOrder(location))
            {
                // Ignore a write if the location is in its assignments.
                if (version.WriteLocation is AssignmentStatement assignment && assignment.Assignees.Contains(location))
                    continue;

                return version;
            }
        }

        return null;
    }
}
