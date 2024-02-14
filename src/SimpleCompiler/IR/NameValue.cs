using SimpleCompiler.Helpers;

namespace SimpleCompiler.IR;

public sealed class NameValue(string name, int version) : Operand
{
    public const string TemporaryName = "t";
    public static NameValue Temporary(int version) => new(TemporaryName, version);

    public string Name { get; } = name;
    public int Version { get; } = version;

    public override string ToString() => $"{Name}{Version.ToSubscript()}";
}
