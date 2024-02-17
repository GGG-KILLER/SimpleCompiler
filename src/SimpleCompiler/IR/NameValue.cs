using SimpleCompiler.Helpers;

namespace SimpleCompiler.IR;

public sealed class NameValue(string name, int version) : Operand
{
    private const string TemporaryName = "t";
    private const int UnversionedVersion = -1;

    public static NameValue Temporary(int version) => new(TemporaryName, version);
    public static NameValue Unversioned(string name) => new(name, UnversionedVersion);

    public string Name { get; } = name;
    public int Version { get; } = version;

    public bool IsTemporary => Name == TemporaryName;
    public bool IsUnversioned => Version == UnversionedVersion;

    public override string ToString() => IsUnversioned ? Name : $"{Name}{Version.ToSubscript()}";
}
