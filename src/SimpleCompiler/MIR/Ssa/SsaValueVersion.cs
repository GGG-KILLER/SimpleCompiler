using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace SimpleCompiler.MIR.Ssa;

public sealed class SsaValueVersion
{
    internal SsaValueVersion(int version, SsaVariable variable, MirNode write, Expression value)
    {
        Version = version;
        Variable = variable;
        WriteLocation = write;
        Value = value;
        PossibleValues = [value];
    }

    internal SsaValueVersion(int version, SsaVariable variable, MirNode writeLocation, IEnumerable<Expression> values)
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

    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsPhi => Value is null && PossibleValues.Length > 1;
    public Expression? Value { get; }
    public ImmutableArray<Expression> PossibleValues { get; }
}
