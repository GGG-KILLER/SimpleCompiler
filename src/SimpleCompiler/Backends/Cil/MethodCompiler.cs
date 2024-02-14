using System.Reflection.Emit;
using Sigil;
using SimpleCompiler.IR;
using SimpleCompiler.Runtime;

namespace SimpleCompiler.Backends.Cil;

internal sealed class MethodCompiler(ModuleBuilder moduleBuilder, IrGraph ir, Emit<Func<LuaValue, LuaValue>> method)
{
    private readonly Scope _scope = new(moduleBuilder);
    private readonly SlotPool _slots = new(method);
    public Emit<Func<LuaValue, LuaValue>> Method => method;

    public static MethodCompiler Create(ModuleBuilder moduleBuilder, TypeBuilder typeBuilder, IrGraph ir, string name) =>
        throw new NotImplementedException();

    public void AddNilReturn()
    {
        method.NewObject<LuaValue>();
        method.Return();
    }

    public void Compile() => throw new NotImplementedException();

    public MethodBuilder CreateMethod() => method.CreateMethod(OptimizationOptions.All);

    public enum EmitOptions
    {
        None = 0,

        NeedsLuaValue = 1 << 1,
        NeedsAddr = 1 << 2,

        NeedsLuaValueAddr = NeedsLuaValue | NeedsAddr
    }
}

internal static class EmitOptionsExtensions
{
    public static bool NeedsLuaValue(this MethodCompiler.EmitOptions options) => (options & MethodCompiler.EmitOptions.NeedsLuaValue) != 0;
    public static bool NeedsAddr(this MethodCompiler.EmitOptions options) => (options & MethodCompiler.EmitOptions.NeedsAddr) != 0;
}
