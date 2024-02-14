namespace SimpleCompiler.IR;

public sealed class Phi(IEnumerable<(int SourceBlockOrdinal, NameValue Value)> names)
{
    public IEnumerable<(int SourceBlockOrdinal, NameValue Value)> Names { get; } = names;

    public override string ToString() => $"ϕ({string.Join(", ", Names)})";
}
