
using System.Reflection;
using System.Reflection.Emit;
using Sigil;
using SimpleCompiler.Runtime;
using SimpleCompiler.IR;
using Lokad.ILPack;
using SimpleCompiler.FileSystem;
using System.Text;
using System.Runtime.Loader;

namespace SimpleCompiler.Backends.Cil;

public sealed partial class CilBackend(TextWriter? cilDebugWriter = null) : IBackend
{
    private void CompileProgram(ModuleBuilder moduleBuilder, IrGraph ir, out MethodInfo entryPoint)
    {
        var programBuilder = moduleBuilder.DefineType(
            "Program",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);

        var luaEntryPoint = EmitLuaEntryPoint(moduleBuilder, programBuilder, ir);
        var dotnetEntryPoint = EmitDotnetEntryPoint(programBuilder, luaEntryPoint);

        var type = programBuilder.CreateType();
        entryPoint = type.GetMethod(dotnetEntryPoint.Name, dotnetEntryPoint.GetParameters().Select(x => x.ParameterType).ToArray())!;
    }

    private Emit<Func<LuaValue, LuaValue>> EmitLuaEntryPoint(ModuleBuilder moduleBuilder, TypeBuilder programBuilder, IrGraph ir)
    {
        var compiler = MethodCompiler.Create(moduleBuilder, programBuilder, ir, "TopLevel");

        compiler.Compile();
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

    public async Task EmitToDirectory(EmitOptions emitOptions, IrGraph ir, IOutputManager output, CancellationToken cancellationToken = default)
    {
        byte[] bytes;
        {
            var assemblyName = new AssemblyName(emitOptions.OutputName);
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name + ".dll");

            CompileProgram(moduleBuilder, ir, out var entryPoint);

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

        // Copy all referenced assemblies.
        {
            var ctx = new AssemblyLoadContext("ExportContext", true);
            Assembly compiled;
            using (var stream = new MemoryStream(bytes, false))
                compiled = ctx.LoadFromStream(stream);

            foreach (var referenced in compiled.GetReferencedAssemblies())
            {
                if (referenced.Name is "System.Runtime" or "System.Private.CoreLib")
                    continue;

                var assembly = ctx.LoadFromAssemblyName(referenced);
                var location = assembly.Location;
                var name = Path.GetFileName(location);

                using var runtimeOutput = output.CreateFile(name);
                using var runtimeInput = File.OpenRead(location);
                await runtimeInput.CopyToAsync(runtimeOutput, 4096, cancellationToken);
            }
        }
    }
}
