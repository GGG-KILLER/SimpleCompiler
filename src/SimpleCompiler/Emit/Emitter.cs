
using System.Reflection;
using System.Reflection.Emit;
using Sigil;
using SimpleCompiler.Runtime;
using SimpleCompiler.IR;
using Lokad.ILPack;
using System.Reflection.Metadata;

namespace SimpleCompiler.Emit;

internal sealed partial class Emitter
{
    private readonly IrTree _tree;
    private readonly ModuleBuilder _moduleBuilder;
    private readonly TypeBuilder _programBuilder;
    private readonly TextWriter? _cilDebugWriter;

    private Emitter(
        IrTree tree,
        ModuleBuilder moduleBuilder,
        TextWriter? cilDebugWriter)
    {
        _scopeStack = new(moduleBuilder);
        _scopeStack.NewScope();

        _moduleBuilder = moduleBuilder;
        _programBuilder = moduleBuilder.DefineType(
            "Program",
            TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit); ;
        _cilDebugWriter = cilDebugWriter;
        _tree = tree;
    }

    private void CompileProgram(out Type programType, out MethodInfo entryPoint)
    {
        var luaEntryPoint = EmitLuaEntryPoint();
        var dotnetEntryPoint = EmitDotnetEntryPoint(luaEntryPoint);

        programType = _programBuilder.CreateType();
        entryPoint = programType.GetMethod(dotnetEntryPoint.Name, dotnetEntryPoint.GetParameters().Select(x => x.ParameterType).ToArray())!;
    }

    private Emit<Func<LuaValue, LuaValue>> EmitLuaEntryPoint()
    {
        var context = PushMethod("toplevel");

        Visit(_tree.Root, EmitOptions.None);

        // Return nil initially while we don't have CFA adding returns where necessary.
        context.Method.NewObject<LuaValue>();
        context.Method.Return();

        if (!ReferenceEquals(PopMethod(), context))
            throw new InvalidOperationException("Popped context is not the entry method.");

        _cilDebugWriter?.WriteLine(context.Method.Instructions());
        context.Method.CreateMethod(OptimizationOptions.All);
        return context.Method;
    }

    private MethodBuilder EmitDotnetEntryPoint(Emit<Func<LuaValue, LuaValue>> luaEntryPoint)
    {
        var method = Emit<Action<string[]>>.BuildStaticMethod(
            _programBuilder,
            "Main",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static);

        // TODO: When we have tables, implement args conversion.
        method.NewObject<LuaValue>();

        method.Call(luaEntryPoint);

        // Pop entry point return since nothing will use it.
        method.Pop();

        // End method with return.
        method.Return();

        _cilDebugWriter?.WriteLine("");
        _cilDebugWriter?.WriteLine("----------- .NET Entry Point ----------");
        _cilDebugWriter?.WriteLine(method.Instructions());

        return method.CreateMethod(OptimizationOptions.All);
    }

    public static async Task EmitAsync(string name, IrTree tree, Stream stream, TextWriter? cilDebugWriter = null)
    {
        var assemblyName = new AssemblyName(name);
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name + ".dll");

        var emitter = new Emitter(tree, moduleBuilder, cilDebugWriter);
        emitter.CompileProgram(out _, out var entryPoint);

        var gen = new AssemblyGenerator();
        var bytes = gen.GenerateAssemblyBytes(
            assemblyBuilder,
            [assemblyBuilder],
            entryPoint);
        await stream.WriteAsync(bytes.AsMemory())
                    .ConfigureAwait(false);
    }
}
