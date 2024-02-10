using System.Reflection;
using System.Reflection.Emit;
using Sigil;
using SimpleCompiler.Helpers;
using SimpleCompiler.IR;
using SimpleCompiler.Runtime;

namespace SimpleCompiler.Emit;

public sealed class ScopeStack(ModuleBuilder moduleBuilder)
{
    private readonly Stack<Scope> _stack = [];
    private readonly Reference<int> _counter = new(0);

    public Scope NewScope()
    {
        var scope = new Scope(moduleBuilder, _stack, _counter);
        _stack.Push(scope);
        return scope;
    }

    private Scope Current => _stack.Peek();

    public Local? GetLocal(VariableInfo variable) => Current.GetLocal(variable);
    public Local GetOrCreateLocal(Emit<Func<LuaValue, LuaValue>> method, VariableInfo variable) => Current.GetOrCreateLocal(method, variable);
    public TypeBuilder GetCacheType(TypeBuilder currentType) => Current.GetCacheType(currentType);
    public FieldBuilder CreateDelegateCache(TypeBuilder currentType, Type delegateType) => Current.CreateDelegateCache(currentType, delegateType);

    public sealed class Scope : IDisposable
    {
        private readonly object _lock = new();
        private readonly ModuleBuilder _moduleBuilder;
        private readonly Stack<Scope> _stack;
        private readonly IReference<int> _globalCounter;
        private int _counter;

        public Dictionary<VariableInfo, Local> Locals { get; } = [];
        private TypeBuilder? _cacheType;

        internal Scope(ModuleBuilder moduleBuilder, Stack<Scope> stack, IReference<int> counter)
        {
            _moduleBuilder = moduleBuilder;
            _stack = stack;
            _globalCounter = counter;
        }

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
                        var n = Interlocked.Increment(ref _globalCounter.AsRef());
                        _cacheType = _moduleBuilder.DefineType($"<>o__{n}", TypeAttributes.Class | TypeAttributes.NestedPrivate | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, currentType);
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

        public void Dispose()
        {
            if (!ReferenceEquals(_stack.Pop(), this))
                throw new InvalidOperationException("Wrong pop order.");
        }
    }
}
