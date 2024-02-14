using SimpleCompiler.IR;

namespace SimpleCompiler.Frontends.Lua;

public sealed class NameTracker
{
    private int _tempCounter;
    private readonly List<NameValue> _nameValues = [];
    private readonly Dictionary<string, int> _versions = [];

    public IReadOnlyList<NameValue> All => _nameValues;

    public NameValue NewValue(string name)
    {
        if (!_versions.TryGetValue(name, out var version))
            version = 1;
        _versions[name] = version + 1;
        var nameValue = new NameValue(name, version);
        _nameValues.Add(nameValue);
        return nameValue;
    }

    public NameValue NewTemporary()
    {
        var nameValue = NameValue.Temporary(Interlocked.Increment(ref _tempCounter));
        _nameValues.Add(nameValue);
        return nameValue;
    }
}
