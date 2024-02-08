using System.Collections.Immutable;

namespace SimpleCompiler.MIR.Ssa;

public sealed class SsaValueVersion
{
    internal SsaValueVersion(int version, SsaVariable variable, MirNode write, MirNode value)
    {
        Version = version;
        Variable = variable;
        WriteLocation = write;
        Value = value;
        PossibleValues = [value];
    }

    internal SsaValueVersion(int version, SsaVariable variable, MirNode writeLocation, IEnumerable<MirNode> values)
    {
        Version = version;
        Variable = variable;
        WriteLocation = writeLocation;
        Value = null;
        PossibleValues = ImmutableArray.CreateRange(values);
    }

    public int Version { get; }
    public SsaVariable Variable { get; }

    public MirNode WriteLocation { get; }

    public bool IsPhi => Value is null && PossibleValues.Length > 1;
    public MirNode? Value { get; }
    public ImmutableArray<MirNode> PossibleValues { get; }
}
