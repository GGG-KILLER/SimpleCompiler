
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Sigil;
using SimpleCompiler.Runtime;

namespace SimpleCompiler.Emit;

internal sealed class MethodContext(ModuleBuilder moduleBuilder, Emit<Func<LuaValue, LuaValue>> method)
{
    private static ConditionalWeakTable<Local, MethodContext> _localOwners = [];

    private readonly Stack<Local> _luaValueSlotPool = [];

    public readonly Emit<Func<LuaValue, LuaValue>> Method = method;

    public readonly Scope Scope = new(moduleBuilder);

    public Local DangerousRentSlot()
    {
        if (!_luaValueSlotPool.TryPop(out var local))
        {
            local = Method.DeclareLocal<LuaValue>();
        }

        _localOwners.Add(local, this);
        return local;
    }

    public LuaValueSlotOwner RentSlot() => new(this, DangerousRentSlot());

    public void ReturnSlot(Local local)
    {
        if (!_localOwners.TryGetValue(local, out var owner) || !ReferenceEquals(this, owner))
            throw new InvalidOperationException("Slot returned to wrong owner.");
        _localOwners.Remove(local);

        // Even though we shouldn't grow unlimitedly as a pool,
        // we also don't want to infinitely create locals, so
        // keep every single local we create to avoid creating
        // new ones whenever possible.
        _luaValueSlotPool.Push(local);
    }

    public void WithSlot(Action<Emit<Func<LuaValue, LuaValue>>, Local> body)
    {
        var local = DangerousRentSlot();
        try
        {
            body(Method, local);
        }
        finally
        {
            ReturnSlot(local);
        }
    }
}

internal sealed class LuaValueSlotOwner(MethodContext context, Local local) : IDisposable
{
    private int _disposed = 0;

    public MethodContext Context { get; } = context;
    public Local Local => _disposed == 0 ? local : throw new ObjectDisposedException(nameof(LuaValueSlotOwner));

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
        {
            Context.ReturnSlot(Local);
        }
    }
}
