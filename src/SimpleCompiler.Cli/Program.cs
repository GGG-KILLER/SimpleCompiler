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
    var compiler = Compiler.Create(new AssemblyName(name), cilDebugWriter);

    s.Restart();
    var mirRoot = compiler.LowerSyntax((LuaSyntaxTree)tree);
    Console.WriteLine($"Syntax lowering done in {Duration.Format(s.Elapsed.Ticks)}");

    if (debug)
        await dumpMir(path, 1, compiler, mirRoot, ctx.CancellationToken);

    s.Restart();
    mirRoot = compiler.Optimize(mirRoot);
    Console.WriteLine($"Optimizing done in {Duration.Format(s.Elapsed.Ticks)}");

    if (debug)
        await dumpMir(path, 2, compiler, mirRoot, ctx.CancellationToken);

    s.Restart();
    var instrs = compiler.LowerMir(mirRoot);
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

    Console.WriteLine(value: "Compiling...");
    s.Restart();
    compiler.CompileProgram(instrs);
    Console.WriteLine($"Compiled in {Duration.Format(s.Elapsed.Ticks)}");
    cilDebugWriter?.Flush();
    cilDebugWriter?.Dispose();

    var outputDir = Path.GetDirectoryName(path);
    var dllPath = Path.ChangeExtension(path, ".dll");
    Console.WriteLine($"Saving to {dllPath}...");
    using (var stream = File.Open(dllPath, FileMode.Create, FileAccess.Write))
    {
        await compiler.SaveAsync(stream);
    }
    await WriteRuntimeConfig(path);

    Console.WriteLine("Copying runtime assembly to same directory...");
    var runtimeAssembly = typeof(LuaValue).Assembly;
    File.Copy(
        runtimeAssembly.Location,
        Path.Combine(outputDir!, Path.GetFileName(runtimeAssembly.Location)),
        true);

    return 0;
});

await app.RunAsync();

static async Task dumpMir(string path, int num, Compiler compiler, MirNode root, CancellationToken cancellationToken = default)
{
    using var writer = File.CreateText(Path.ChangeExtension(path, $"{num}.mir"));

    var indentedWriter = new IndentedTextWriter(writer, "    ");
    var debugWriter = new MirDebugPrinter(indentedWriter);
    indentedWriter.Write("Global Scope: ");
    debugWriter.WriteScope(compiler.GlobalScope);
    indentedWriter.WriteLine();
    debugWriter.Visit(root);

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