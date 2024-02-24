namespace SimpleCompiler.IR;

public sealed class Phi(List<(int SourceBlockOrdinal, Operand Value)> names)
{
    public List<(int SourceBlockOrdinal, Operand Value)> Values { get; } = names;

    public override string ToString() =>
        $"ϕ({string.Join(", ", Values.Select(p => $"[BB{p.SourceBlockOrdinal}: {p.Value}]"))})";
}
