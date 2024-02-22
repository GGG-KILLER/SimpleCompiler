using System.Reflection;
using System.Reflection.Emit;

namespace SimpleCompiler.Backends.Cil;

internal sealed class Scope(ModuleBuilder moduleBuilder)
{
    private static int s_globalCounter;
    private readonly object _lock = new();
    private TypeBuilder? _cacheType;
    private int _counter;

    public TypeBuilder GetCacheType(TypeBuilder currentType)
    {
        if (_cacheType is null)
        {
            lock (_lock)
            {
                if (_cacheType is null)
                {
                    var n = Interlocked.Increment(ref s_globalCounter);
                    _cacheType = moduleBuilder.DefineType($"<>o__{n}", TypeAttributes.Class | TypeAttributes.NestedPrivate | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, currentType);
                }
            }
        }
        return _cacheType;
    }

    public FieldBuilder CreateDelegateCache(TypeBuilder currentType, Type delegateType)
    {
        var cache = GetCacheType(currentType);
        var c = Interlocked.Increment(ref _counter);
        return cache.DefineField($"<>p__{c}", delegateType, FieldAttributes.Public | FieldAttributes.Static);
    }
}
