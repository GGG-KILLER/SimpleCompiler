using System.Collections.Immutable;

namespace SimpleCompiler.IR;

public sealed class Phi(ImmutableArray<(int SourceBlockOrdinal, NameValue Value)> names)
{
    public ImmutableArray<(int SourceBlockOrdinal, NameValue Value)> Values { get; } = names;

    public override string ToString() =>
        $"ϕ({string.Join(", ", Values.Select(p => $"[BB{p.SourceBlockOrdinal}: {p.Value}]"))})";
}
