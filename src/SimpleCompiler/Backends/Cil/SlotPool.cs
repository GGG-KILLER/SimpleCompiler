using System.Runtime.CompilerServices;
using Sigil;
using SimpleCompiler.Runtime;

namespace SimpleCompiler.Backends.Cil;

internal sealed class SlotPool(Emit<Func<LuaValue, LuaValue>> method)
{
    private static ConditionalWeakTable<Local, SlotPool> _localOwners = [];
    private readonly Stack<Local> _luaValueSlotPool = [];

    public Local DangerousRentSlot()
    {
        if (!_luaValueSlotPool.TryPop(out var local))
        {
            local = method.DeclareLocal<LuaValue>();
        }

        _localOwners.Add(local, this);
        return local;
    }

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
            body(method, local);
        }
        finally
        {
            ReturnSlot(local);
        }
    }
}