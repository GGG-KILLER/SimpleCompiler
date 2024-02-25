using System.Diagnostics;
using System.Runtime.CompilerServices;
using Sigil;
using SimpleCompiler.Helpers;
using SimpleCompiler.Runtime;

namespace SimpleCompiler.Backends.Cil;

internal sealed class SlotPool
{
    private static readonly ConditionalWeakTable<Local, SlotPool> s_localOwners = [];
    private static readonly ConditionalWeakTable<Local, object> s_localTypes = [];
    private readonly Stack<Local>[] _luaValueSlotPool;
    private readonly Emit<Func<LuaValue, LuaValue>> _method;

    public SlotPool(Emit<Func<LuaValue, LuaValue>> method)
    {
        _method = method;
        _luaValueSlotPool = new Stack<Local>[5];
        for (var idx = 0; idx < _luaValueSlotPool.Length; idx++)
            _luaValueSlotPool[idx] = new Stack<Local>();
    }

    public Local DangerousRentSlot(LocalType type)
    {
        if (type == LocalType.None)
            throw new ArgumentException("None is not a valid type for a local.", nameof(type));

        if (!_luaValueSlotPool[(int) type].TryPop(out var local))
        {
            local = _method.DeclareLocal(type.GetClrType());
        }

        s_localOwners.Add(local, this);
        return local;
    }

    public void ReturnSlot(Local local)
    {
        if (!s_localOwners.TryGetValue(local, out var owner) || !ReferenceEquals(this, owner))
            throw new InvalidOperationException("Slot returned to wrong owner.");
        s_localOwners.Remove(local);

        LocalType type = LocalType.None;
        if (s_localTypes.TryGetValue(local, out var boxedType))
            type = CastHelper.FastUnbox<LocalType>(boxedType);
        if (type == LocalType.None)
            return;

        // Even though we shouldn't grow unlimitedly as a pool,
        // we also don't want to infinitely create locals, so
        // keep every single local we create to avoid creating
        // new ones whenever possible.
        _luaValueSlotPool[(int) type].Push(local);
    }

    public void WithSlot(LocalType type, Action<Emit<Func<LuaValue, LuaValue>>, Local> body)
    {
        var local = DangerousRentSlot(type);
        try
        {
            body(_method, local);
        }
        finally
        {
            ReturnSlot(local);
        }
    }
}

internal enum LocalType
{
    None = 0,

    Bool,
    Long,
    Double,
    String,
    LuaFunction,
    LuaValue,
}

internal static class LocalTypeExtensions
{
    public static Type GetClrType(this LocalType type) =>
        type switch
        {
            LocalType.Bool => typeof(bool),
            LocalType.Long => typeof(long),
            LocalType.Double => typeof(double),
            LocalType.String => typeof(string),
            LocalType.LuaFunction => typeof(LuaFunction),
            LocalType.LuaValue => typeof(LuaValue),
            _ => throw new UnreachableException()
        };

    public static ValueKind ToValueKind(this LocalType type) =>
        type switch
        {
            LocalType.Bool => ValueKind.Boolean,
            LocalType.Long => ValueKind.Long,
            LocalType.Double => ValueKind.Double,
            LocalType.String => ValueKind.String,
            LocalType.LuaFunction => ValueKind.String,
            _ => throw new ArgumentException($"No ValueKind for {type}.")
        };
}
