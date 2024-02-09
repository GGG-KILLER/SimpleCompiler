// See https://aka.ms/new-console-template for more information

using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using Cocona;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Text;
using SimpleCompiler.Cli.Validation;
using SimpleCompiler.Compiler;
using SimpleCompiler.MIR;
using SimpleCompiler.MIR.Ssa;
using SimpleCompiler.Runtime;
using Tsu.Numerics;

var app = CoconaLiteApp.Create(args);

app.AddCommand("run", (
    [Argument][FileExists] string path
) =>
{
    var assembly = Assembly.LoadFrom(path);
    var program = assembly.GetType("Program");
    if (program is null)
    {
        Console.Error.WriteLine("Unable to find Program entry class.");
        return 1;
    }
    var entry = program.GetMethod("Main");
    if (entry is null)
    {
        Console.Error.WriteLine("Unable to find Program.Main entry class.");
        return 2;
    }

    try
    {
        entry.Invoke(null, null);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }

    return 0;
});

app.AddCommand("build", async (
    [Argument][FileExists] string path,
    [Option('d')] bool debug,
    CoconaAppContext ctx) =>
{
    SourceText sourceText;
    using (var stream = File.OpenRead(path))
        sourceText = SourceText.From(stream, throwIfBinaryDetected: true);

    var s = Stopwatch.StartNew();
    var tree = LuaSyntaxTree.ParseText(sourceText, new LuaParseOptions(LuaSyntaxOptions.Lua51), path, ctx.CancellationToken);
    Console.WriteLine($"Parsed input file in {Duration.Format(s.Elapsed.Ticks)}");

    if (tree.GetDiagnostics().Any())
    {
        foreach (var diagnostic in tree.GetDiagnostics())
        {
            Console.Error.WriteLine(diagnostic.ToString());
        }
        return 1;
    }

    var name = Path.GetFileNameWithoutExtension(path);
    TextWriter? cilDebugWriter = null;
    if (debug)
        cilDebugWriter = File.CreateText(Path.ChangeExtension(path, ".cil"));
    var compilation = new Compilation(tree);

    s.Restart();
    var mirTree = compilation.LowerSyntax();
    Console.WriteLine($"Syntax lowering done in {Duration.Format(s.Elapsed.Ticks)}");

    if (debug)
    {
        s.Restart();
        mirTree.Ssa.Compute();
        Console.WriteLine($"  SSA computation done in {Duration.Format(s.Elapsed.Ticks)}");

        await dumpMir(path, 1, mirTree, ctx.CancellationToken);
    }

    s.Restart();
    Console.WriteLine($"Optimizing...");
    var c = 2;
    mirTree = compilation.OptimizeLoweredSyntax((root, stage) =>
    {
        Console.WriteLine($"  {c}: {stage}");
        dumpMir(path, c++, MirTree.FromRoot(mirTree.GlobalScope, root), ctx.CancellationToken).GetAwaiter().GetResult();
    });
    Console.WriteLine($"  Done in {Duration.Format(s.Elapsed.Ticks)}");

    if (debug)
    {
        s.Restart();
        mirTree.Ssa.Compute();
        Console.WriteLine($"  SSA computation done in {Duration.Format(s.Elapsed.Ticks)}");

        await dumpMir(path, c++, mirTree, ctx.CancellationToken);
    }

    s.Restart();
    var instrs = compilation.LowerMir();
    Console.WriteLine($"MIR lowering done in {Duration.Format(s.Elapsed.Ticks)}");
    if (debug)
    {
        using var writer = File.CreateText(Path.ChangeExtension(path, ".lir"));
        foreach (var instr in instrs)
        {
            await writer.WriteLineAsync(instr.ToRepr().AsMemory(), ctx.CancellationToken)
                        .ConfigureAwait(false);
        }
    }

    var outputDir = Path.GetDirectoryName(path);
    var dllPath = Path.ChangeExtension(path, ".dll");
    Console.WriteLine($"Compiling into {dllPath}...");
    using (var stream = File.Open(dllPath, FileMode.Create, FileAccess.Write))
    {
        await compilation.EmitAsync(name, stream, cilDebugWriter)
                         .ConfigureAwait(false);
    }
    await WriteRuntimeConfig(path);
    Console.WriteLine($"Compiled in {Duration.Format(s.Elapsed.Ticks)}");
    cilDebugWriter?.Flush();
    cilDebugWriter?.Dispose();

    Console.WriteLine("Copying runtime assembly to same directory...");
    var runtimeAssembly = typeof(LuaValue).Assembly;
    File.Copy(
        runtimeAssembly.Location,
        Path.Combine(outputDir!, Path.GetFileName(runtimeAssembly.Location)),
        true);

    return 0;
});

await app.RunAsync();

static async Task dumpMir(string path, int num, MirTree tree, CancellationToken cancellationToken = default)
{
    using var writer = File.CreateText(Path.ChangeExtension(path, $"{num}.mir"));

    var indentedWriter = new IndentedTextWriter(writer, "    ");
    var debugWriter = new MirDebugPrinter(indentedWriter, tree.Ssa);
    indentedWriter.Write("Global Scope: ");
    debugWriter.WriteScope(tree.GlobalScope);
    indentedWriter.WriteLine();
    debugWriter.Visit(tree.Root);

    await indentedWriter.FlushAsync(cancellationToken)
                        .ConfigureAwait(false);
}

static Task WriteRuntimeConfig(string path)
{
    return File.WriteAllTextAsync(Path.ChangeExtension(path, ".runtimeconfig.json"), $$"""
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
    """);
}
