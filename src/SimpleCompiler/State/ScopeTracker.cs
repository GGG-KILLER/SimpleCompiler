using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using SimpleCompiler.Helpers;
using RE = System.Reflection.Emit;

namespace SimpleCompiler;

public sealed class ScopeStack(ModuleBuilder moduleBuilder)
{
    private readonly Stack<Scope> _stack = [];
    private readonly Reference<int> _counter = new(0);

    public Scope Current => _stack.Peek();

    public IDisposable NewScope()
    {
        var scope = new Scope(moduleBuilder, _stack, _counter);
        _stack.Push(scope);
        return scope;
    }

    public void AssignLabel(string name, Label label) =>
        Current.AssignLabel(name, label);

    public sealed class Scope : IDisposable
    {
        private readonly object _lock = new();
        private readonly ModuleBuilder _moduleBuilder;
        private readonly Stack<Scope> _stack;
        private readonly IReference<int> _globalCounter;
        private int _counter;

        public Dictionary<string, Label> Labels { get; } = [];
        private TypeBuilder? _callsiteCache;

        internal Scope(ModuleBuilder moduleBuilder, Stack<Scope> stack, IReference<int> counter)
        {
            _moduleBuilder = moduleBuilder;
            _stack = stack;
            _globalCounter = counter;
        }

        public void AssignLabel(string name, Label label) =>
            Labels.Add(name, label);

        public TypeBuilder GetCallsiteCache(TypeBuilder currentType)
        {
            if (_callsiteCache is null)
            {
                lock (_lock)
                {
                    if (_callsiteCache is null)
                    {
                        var n = Interlocked.Increment(ref _globalCounter.AsRef());
                        _callsiteCache = _moduleBuilder.DefineType($"<>o__{n}", TypeAttributes.Class | TypeAttributes.NestedPrivate | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, currentType);
                    }
                }
            }
            return _callsiteCache;
        }

        public FieldBuilder CreateCallsiteCache(TypeBuilder currentType, Type[] args, Type ret)
        {
            var cache = GetCallsiteCache(currentType);
            var funcT = TypeHelper.GetFuncType([typeof(CallSite), .. args], ret);
            var callsiteT = typeof(CallSite<>).MakeGenericType(funcT);

            var c = Interlocked.Increment(ref _counter);
            var field = cache.DefineField($"<>p__{c}", callsiteT, FieldAttributes.Public | FieldAttributes.Static);
            field.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(NullableAttribute).GetConstructor([typeof(byte[])])!,
                [TypeHelper.GenerateNullabeAttributeBytes([callsiteT])]));

            return field;
        }

        public void Dispose()
        {
            if (!ReferenceEquals(_stack.Pop(), this))
                throw new InvalidOperationException("Wrong pop order.");
        }
    }
}
