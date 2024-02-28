
using System.Reflection;
using Sigil;
using SimpleCompiler.Runtime;
using SimpleCompiler.IR;
using Lokad.ILPack;
using SimpleCompiler.FileSystem;
using System.Text;
using System.Runtime.Loader;
using SimpleCompiler.Backend.Cil.Emit;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;

namespace SimpleCompiler.Backends.Cil;

public sealed partial class CilBackend(TextWriter? cilDebugWriter = null) : IBackend
{
    private void CompileProgram(ModuleBuilder moduleBuilder, IrGraph ir, out int entryPointToken)
    {
        var programBuilder = moduleBuilder.DefineType(
            "Program",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);

        var luaEntryPoint = EmitLuaEntryPoint(programBuilder, ir);
        entryPointToken = EmitDotnetEntryPoint(programBuilder, luaEntryPoint);

        programBuilder.CreateType();
    }

    private MethodBuilder EmitLuaEntryPoint(TypeBuilder programBuilder, IrGraph ir)
    {
        var symbolTable = InformationCollector.CollectSymbolInfomation(ir);
        SsaDestructor.DestructSsa(ir);
        var compiler = MethodCompiler.Create(programBuilder, ir, symbolTable, "TopLevel");

        compiler.Compile();
        cilDebugWriter?.WriteLine(compiler.Method.Instructions());

        compiler.CreateMethod();
        return compiler.Method;
    }

    private int EmitDotnetEntryPoint(TypeBuilder programBuilder, MethodBuilder luaEntryPoint)
    {
        var method = programBuilder.DefineMethod(
            "Main",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static);
        var builder = method.GetILGenerator();

        // TODO: When we have tables, implement args conversion.
        builder.Emit(OpCodes.Newobj, typeof(LuaValue));

        builder.EmitCall(OpCodes.Call, luaEntryPoint, null);

        // Pop entry point return since nothing will use it.
        builder.Emit(OpCodes.Pop);

        // End method with return.
        builder.Emit(OpCodes.Ret);

        return method.MetadataToken;
    }

    public async Task EmitToDirectory(EmitOptions emitOptions, IrGraph ir, IOutputManager output, CancellationToken cancellationToken = default)
    {
        byte[] bytes;
        {
            var assemblyName = new AssemblyName(emitOptions.OutputName);
            var assemblyBuilder = new AssemblyBuilderImpl(assemblyName, typeof(object).Assembly);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name + ".dll");

            CompileProgram(moduleBuilder, ir, out var entryPoint);

            using var memStream = new MemoryStream();
            assemblyBuilder.Save(memStream);
            bytes = memStream.ToArray();

            memStream.Seek(0, SeekOrigin.Begin);
            if (cilDebugWriter is not null)
            {
                var file = new PEFile(moduleBuilder.Name, memStream);
                var outputWriter = new PlainTextOutput(cilDebugWriter);
                var disassembler = new ReflectionDisassembler(outputWriter, cancellationToken);

                disassembler.WriteAssemblyHeader(file);
                outputWriter.WriteLine();
                disassembler.WriteModuleContents(file);

                memStream.Seek(0, SeekOrigin.Begin);
            }

            using var stream = output.CreateFile($"{emitOptions.OutputName}.dll");
            await memStream.CopyToAsync(stream, cancellationToken);
            // Take all dynamic stuff out of scope by here
            // so we don't use more memory than necessary.
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
