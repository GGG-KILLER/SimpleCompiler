using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Sigil;
using Sigil.NonGeneric;
using SimpleCompiler.Helpers;
using SimpleCompiler.LIR;
using SimpleCompiler.MIR;
using SimpleCompiler.Runtime;
using Label = Sigil.Label;

namespace SimpleCompiler.Compiler;

public sealed class ScopeStack(ModuleBuilder moduleBuilder)
{
    private readonly Stack<Scope> _stack = [];
    private readonly Reference<int> _counter = new(0);

    public Scope Current => _stack.Peek();

    public Scope NewScope()
    {
        var scope = new Scope(moduleBuilder, _stack, _counter, _stack.TryPeek(out var parent) ? parent : null);
        _stack.Push(scope);
        return scope;
    }

    public void AssignLabel(Location location, Label label) =>
        Current.AssignLabel(location, label);

    public sealed class Scope : IDisposable
    {
        private readonly object _lock = new();
        private readonly ModuleBuilder _moduleBuilder;
        private readonly Stack<Scope> _stack;
        private readonly IReference<int> _globalCounter;
        private readonly Scope? _parent;
        private int _counter;

        public Dictionary<Location, Label> Labels { get; } = [];
        public Dictionary<VariableInfo, Local> Locals { get; } = [];
        private TypeBuilder? _callsiteCache;

        internal Scope(ModuleBuilder moduleBuilder, Stack<Scope> stack, IReference<int> counter, Scope? parent)
        {
            _moduleBuilder = moduleBuilder;
            _stack = stack;
            _globalCounter = counter;
            _parent = parent;
        }

        public void AssignLabel(Location location, Label label) =>
            Labels.Add(location, label);

        public Label GetOrCreateLabel(Emit method, Location location)
        {
            if (!Labels.TryGetValue(location, out var label))
                Labels[location] = label = method.DefineLabel();
            return label;
        }

        public Local? GetLocal(VariableInfo variable) =>
            Locals.GetValueOrDefault(variable) ?? _parent?.GetLocal(variable);

        public Local GetOrCreateLocal(Emit method, VariableInfo variable)
        {
            if (GetLocal(variable) is not { } local)
                local = Locals[variable] = method.DeclareLocal<LuaValue>();
            return local;
        }

        public TypeBuilder GetCacheType(TypeBuilder currentType)
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

        public FieldBuilder CreateDelegateCache(TypeBuilder currentType, Type delegateType)
        {
            var cache = GetCacheType(currentType);
            var c = Interlocked.Increment(ref _counter);
            return cache.DefineField($"<>p__{c}", delegateType, FieldAttributes.Public | FieldAttributes.Static);
        }

        public FieldBuilder CreateCallsiteCache(TypeBuilder currentType, Type[] args, Type ret)
        {
            var cache = GetCacheType(currentType);
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
