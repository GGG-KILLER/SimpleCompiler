using System.Reflection;
using System.Reflection.Emit;
using Sigil;
using SimpleCompiler.IR;
using SimpleCompiler.Runtime;

namespace SimpleCompiler.Backends.Cil;

internal sealed class Scope(ModuleBuilder moduleBuilder)
{
    private static int s_globalCounter;
    private readonly object _lock = new();
    private int _counter;

    public Dictionary<VariableInfo, Local> Locals { get; } = [];
    private TypeBuilder? _cacheType;

    public Local? GetLocal(VariableInfo variable) =>
        Locals.GetValueOrDefault(variable);

    public Local GetOrCreateLocal(Emit<Func<LuaValue, LuaValue>> method, VariableInfo variable)
    {
        if (GetLocal(variable) is not { } local)
            local = Locals[variable] = method.DeclareLocal<LuaValue>($"{variable.Name}_{variable.Scope.GetHashCode():X}");
        return local;
    }

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
