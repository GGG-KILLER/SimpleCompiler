using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using SimpleCompiler.IR;

namespace SimpleCompiler.Backends.Cil;

internal sealed class SymbolTable : IReadOnlyDictionary<NameValue, SymbolData>
{
    private readonly Dictionary<NameValue, SymbolData> _data = [];

    public SymbolData this[NameValue name] => _data.TryGetValue(name, out var data) ? data : _data[name] = new SymbolData(name);
    public IEnumerable<NameValue> Keys => _data.Keys;
    public IEnumerable<SymbolData> Values => _data.Values;
    public int Count => _data.Count;

    public bool TryGetValue(NameValue key, [MaybeNullWhen(false)] out SymbolData value) => _data.TryGetValue(key, out value);
    public bool ContainsKey(NameValue key) => _data.ContainsKey(key);
    public IEnumerator<KeyValuePair<NameValue, SymbolData>> GetEnumerator() => _data.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal sealed class SymbolData(NameValue name)
{
    public NameValue Name { get; } = name;
    public SymbolType Types { get; set; } = SymbolType.All;
    public LocalType LocalType => Types.ToLocalType();

    public override string ToString() => $"{Name} (Types: [{Types}], Local Type: {LocalType})";
    private string GetDebuggerDisplay() => ToString();
}

[Flags]
internal enum SymbolType : byte
{
    None = 0,
    Nil = 1 << 1,
    Boolean = 1 << 2,
    Long = 1 << 3,
    Double = 1 << 4,
    String = 1 << 5,
    Function = 1 << 6,

    All = Nil | Boolean | Long | Double | String | Function,
}

internal static class SymbolTypeExtensions
{
    public static bool IsMixed(this SymbolType type) =>
        type is not (SymbolType.Nil or SymbolType.Boolean or SymbolType.Long or SymbolType.Double or SymbolType.String or SymbolType.Function);

    public static LocalType ToLocalType(this SymbolType type) =>
        type switch
        {
            SymbolType.Boolean => LocalType.Bool,
            SymbolType.Long => LocalType.Long,
            SymbolType.Double => LocalType.Double,
            SymbolType.String => LocalType.String,
            SymbolType.Function => LocalType.LuaFunction,
            _ => LocalType.LuaValue,
        };
}
