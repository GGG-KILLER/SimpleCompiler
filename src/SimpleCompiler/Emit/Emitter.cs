namespace SimpleCompiler.Emit;

using System.Reflection;
using System.Reflection.Emit;
using Sigil;
using Sigil.NonGeneric;
using SimpleCompiler.Runtime;
using SimpleCompiler.IR;
using Lokad.ILPack;
using System.Threading.Tasks;

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
        var method = PushMethod(typeof(void), "Main", []);

        Visit(_tree.Root);
        method.Return();

        if (!ReferenceEquals(PopMethod(), method))
            throw new InvalidOperationException("Popped method is not the entry method.");

        _cilDebugWriter?.WriteLine(method.Instructions());
        method.CreateMethod(OptimizationOptions.All);

        programType = _programBuilder.CreateType();
        entryPoint = programType.GetMethod("Main", [])!;
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
            [typeof(string).Assembly, typeof(LuaValue).Assembly],
            entryPoint);
        await stream.WriteAsync(bytes.AsMemory())
                    .ConfigureAwait(false);
    }
}
