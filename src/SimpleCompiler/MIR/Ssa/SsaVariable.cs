namespace SimpleCompiler.MIR.Ssa;

public sealed class SsaVariable
{
    private readonly List<SsaBlock> _referencedBlocks = [];
    private readonly List<SsaValueVersion> _valueVersions = [];

    internal SsaVariable(VariableInfo variable, SsaBlock block)
    {
        Variable = variable;
        DeclaredBlock = block;
    }

    public VariableInfo Variable { get; }
    public SsaBlock DeclaredBlock { get; }
    public IReadOnlyList<SsaBlock> ReferencedBlocks => _referencedBlocks;
    public IReadOnlyList<SsaValueVersion> ValueVersions => _valueVersions;

    internal void AddValueVersion(MirNode write, Expression value) =>
        _valueVersions.Add(new SsaValueVersion(_valueVersions.Count + 1, this, write, value));

    internal void AddPhiVersion(MirNode write, IEnumerable<Expression> values) =>
        _valueVersions.Add(new SsaValueVersion(_valueVersions.Count + 1, this, write, values));

    internal void AddReferencedBlock(SsaBlock block)
    {
        if (DeclaredBlock == block)
            return;

        _referencedBlocks.Add(block);
    }

    public SsaValueVersion? GetVersionAtPoint(MirNode location)
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
