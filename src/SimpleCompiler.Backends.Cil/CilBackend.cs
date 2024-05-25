
using System.Reflection;
using SimpleCompiler.Runtime;
using SimpleCompiler.IR;
using SimpleCompiler.FileSystem;
using System.Text;
using System.Runtime.Loader;
using SimpleCompiler.Backend.Cil.Emit;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Reflection.Emit;

namespace SimpleCompiler.Backends.Cil;

public sealed partial class CilBackend(TextWriter? cilDebugWriter = null) : IBackend
{
    private static void CompileProgram(ModuleBuilder moduleBuilder, IrGraph ir, out MethodBuilder entryPointToken)
    {
        var programBuilder = moduleBuilder.DefineType(
            "Program",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);

        var luaEntryPoint = EmitLuaEntryPoint(programBuilder, ir);
        entryPointToken = EmitDotnetEntryPoint(programBuilder, luaEntryPoint);

        programBuilder.CreateType();
    }

    private static MethodBuilder EmitLuaEntryPoint(TypeBuilder programBuilder, IrGraph ir)
    {
        var symbolTable = InformationCollector.CollectSymbolInfomation(ir);
        SsaDestructor.DestructSsa(ir);

        var compiler = MethodCompiler.Create(programBuilder, ir, symbolTable, "TopLevel");
        compiler.Compile();
        return compiler.Method;
    }

    private static MethodBuilder EmitDotnetEntryPoint(TypeBuilder programBuilder, MethodBuilder luaEntryPoint)
    {
        var method = programBuilder.DefineMethod(
            "Main",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static);
        var builder = method.GetILGenerator();

        // TODO: When we have tables, implement args conversion.
        builder.EmitCall(OpCodes.Call, ReflectionData.ArgumentSpan_Empty, null);

        builder.EmitCall(OpCodes.Call, luaEntryPoint, null);

        // Pop entry point return since nothing will use it.
        builder.Emit(OpCodes.Pop);

        // End method with return.
        builder.Emit(OpCodes.Ret);

        return method;
    }

    public async Task EmitToDirectory(EmitOptions emitOptions, IrGraph ir, IOutputManager output, CancellationToken cancellationToken = default)
    {
        using var memStream = new MemoryStream();
        {
            var assemblyName = new AssemblyName(emitOptions.OutputName);
            var assemblyBuilder = new PersistedAssemblyBuilder(assemblyName, typeof(object).Assembly);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name + ".dll");

            CompileProgram(moduleBuilder, ir, out var entryPoint);

            var metadataBuilder = assemblyBuilder.GenerateMetadata(out var ilStream, out var mappedFieldData);

            var peHeaderBuilder = new PEHeaderBuilder(
                // For now only support DLL, DLL files are considered executable files
                // for almost all purposes, although they cannot be directly run.
                imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll);

            var peBuilder = new ManagedPEBuilder(
                header: peHeaderBuilder,
                metadataRootBuilder: new MetadataRootBuilder(metadataBuilder),
                ilStream: ilStream,
                mappedFieldData: mappedFieldData,
                entryPoint: MetadataTokens.MethodDefinitionHandle(entryPoint.MetadataToken));

            // Write executable into the specified stream.
            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob);
            peBlob.WriteContentTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);

            if (cilDebugWriter is not null)
            {
                using var file = new PEFile(moduleBuilder.Name, new PEReader(memStream, PEStreamOptions.LeaveOpen));
                var outputWriter = new PlainTextOutput(cilDebugWriter);
                var disassembler = new ReflectionDisassembler(outputWriter, cancellationToken);

                disassembler.WriteAssemblyHeader(file);
                outputWriter.WriteLine();
                disassembler.WriteModuleContents(file);
                await cilDebugWriter.FlushAsync(cancellationToken);

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
            memStream.Seek(0, SeekOrigin.Begin);
            Assembly compiled = ctx.LoadFromStream(memStream);

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
