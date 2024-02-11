
using System.Reflection;
using System.Reflection.Emit;
using Sigil;
using SimpleCompiler.Runtime;
using SimpleCompiler.IR;
using Lokad.ILPack;
using SimpleCompiler.FileSystem;
using System.Text;

namespace SimpleCompiler.Backends.Cil;

public sealed partial class CilBackend(TextWriter? cilDebugWriter = null) : IBackend
{
    private void CompileProgram(ModuleBuilder moduleBuilder, IrTree tree, out MethodInfo entryPoint)
    {
        var programBuilder = moduleBuilder.DefineType(
            "Program",
            TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);

        var luaEntryPoint = EmitLuaEntryPoint(moduleBuilder, programBuilder, tree);
        var dotnetEntryPoint = EmitDotnetEntryPoint(programBuilder, luaEntryPoint);

        var type = programBuilder.CreateType();
        entryPoint = type.GetMethod(dotnetEntryPoint.Name, dotnetEntryPoint.GetParameters().Select(x => x.ParameterType).ToArray())!;
    }

    private Emit<Func<LuaValue, LuaValue>> EmitLuaEntryPoint(ModuleBuilder moduleBuilder, TypeBuilder programBuilder, IrTree tree)
    {
        var compiler = MethodCompiler.Create(moduleBuilder, programBuilder, tree, "TopLevel");

        compiler.Visit(tree.Root, MethodCompiler.EmitOptions.None);

        // Return nil initially while we don't have CFA adding returns where necessary.
        compiler.AddNilReturn();

        cilDebugWriter?.WriteLine(compiler.Method.Instructions());
        compiler.CreateMethod();
        return compiler.Method;
    }

    private MethodBuilder EmitDotnetEntryPoint(TypeBuilder programBuilder, Emit<Func<LuaValue, LuaValue>> luaEntryPoint)
    {
        var method = Emit<Action<string[]>>.BuildStaticMethod(
            programBuilder,
            "Main",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static);

        // TODO: When we have tables, implement args conversion.
        method.NewObject<LuaValue>();

        method.Call(luaEntryPoint);

        // Pop entry point return since nothing will use it.
        method.Pop();

        // End method with return.
        method.Return();

        cilDebugWriter?.WriteLine("");
        cilDebugWriter?.WriteLine("----------- .NET Entry Point ----------");
        cilDebugWriter?.WriteLine(method.Instructions());

        return method.CreateMethod(OptimizationOptions.All);
    }

    public async Task EmitToDirectory(EmitOptions emitOptions, IrTree tree, IOutputManager output, CancellationToken cancellationToken = default)
    {
        byte[] bytes;
        {
            var assemblyName = new AssemblyName(emitOptions.OutputName);
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name + ".dll");

            CompileProgram(moduleBuilder, tree, out var entryPoint);

            var gen = new AssemblyGenerator();
            bytes = gen.GenerateAssemblyBytes(
                assemblyBuilder,
                [],
                entryPoint);

            // Take all dynamic stuff out of scope by here
            // so we don't use more memory than necessary.
        }

        using (var stream = output.CreateFile($"{emitOptions.OutputName}.dll"))
        {
            await stream.WriteAsync(bytes.AsMemory(), cancellationToken)
                        .ConfigureAwait(false);
        }

        using (var stream = output.CreateFile($"{emitOptions.OutputName}.runtimeconfig.json"))
        {
            var text = $$"""
            {
              "runtimeOptions": {
                "tfm": "net{{Environment.Version.ToString(2)}}",
                "framework": {
                  "name": "Microsoft.NETCore.App",
                  "version": "{{Environment.Version.ToString(3)}}"
                },
                "configProperties": {
                  "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": false
                }
              }
            }
            """;

            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            await writer.WriteAsync(text);
        }

        var runtime = typeof(LuaValue).Assembly.Location;
        using var runtimeOutput = output.CreateFile(Path.GetFileName(runtime));
        using var runtimeInput = File.OpenRead(runtime);
        await runtimeInput.CopyToAsync(runtimeOutput, 4096, cancellationToken);
    }
}
