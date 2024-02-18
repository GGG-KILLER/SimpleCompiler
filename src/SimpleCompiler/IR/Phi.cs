namespace SimpleCompiler.IR;

public sealed class Phi(List<(int SourceBlockOrdinal, NameValue Value)> names)
{
    public List<(int SourceBlockOrdinal, NameValue Value)> Values { get; } = names;

    public override string ToString() =>
        $"ϕ({string.Join(", ", Values.Select(p => $"[BB{p.SourceBlockOrdinal}: {p.Value}]"))})";
}
