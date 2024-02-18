using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using Cocona;
using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Text;
using SimpleCompiler;
using SimpleCompiler.Backends.Cil;
using SimpleCompiler.Cli;
using SimpleCompiler.Cli.Validation;
using SimpleCompiler.Frontends.Lua;
using SimpleCompiler.IR;
using Tsu.Numerics;

await CoconaLiteApp.RunAsync(async (
    [Argument][FileExists] string path,
    [Option("lua")] string luaVersion,
    [Option('d')] bool debug,
    [Option('O')] bool optimize,
    CoconaAppContext ctx) =>
{
    SourceText sourceText;
    using (var stream = File.OpenRead(path))
        sourceText = SourceText.From(stream, throwIfBinaryDetected: true);

    var presetField = typeof(LuaSyntaxOptions).GetField(luaVersion, BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.IgnoreCase);
    if (presetField is null)
    {
        await Console.Error.WriteLineAsync($"Preset for Lua version {luaVersion} was not found.");
        return -1;
    }
    var preset = (LuaSyntaxOptions) presetField.GetValue(null)!;

    var s = Stopwatch.StartNew();
    var syntaxTree = LuaSyntaxTree.ParseText(sourceText, new LuaParseOptions(preset), path, ctx.CancellationToken);
    Console.WriteLine($"Parsed input file in {Duration.Format(s.Elapsed.Ticks)}");

    if (syntaxTree.GetDiagnostics().Any())
    {
        foreach (var diagnostic in syntaxTree.GetDiagnostics())
        {
            Console.Error.WriteLine(diagnostic.ToString());
        }
        return 1;
    }

    var objDir = ObjectFileManager.Create(Path.Combine(Path.GetDirectoryName(path) ?? ".", "obj"));

    var name = Path.GetFileNameWithoutExtension(path);

    var luaFrontend = new LuaFrontend();
    TextWriter? cilDebugWriter = null;
    if (debug)
        cilDebugWriter = objDir.CreateText(name + ".cil");
    var cilBackend = new CilBackend(cilDebugWriter);

    var compilation = new Compilation<SyntaxTree>(luaFrontend, cilBackend);

    s.Restart();
    var ir = compilation.GetIrGraph(syntaxTree);
    Console.WriteLine($"Syntax lowering done in {Duration.Format(s.Elapsed.Ticks)}");

    if (debug)
    {
        await dumpIr(objDir, name, 1, ir, ctx.CancellationToken);
    }

    var c = 2;
    if (optimize)
    {
        s.Restart();
        Console.WriteLine($"Optimizing...");
        ir = compilation.GetOptimizedIrGraph(syntaxTree, debug ? (ir, stage) =>
        {
            Console.WriteLine($"  {c}: {stage}");

            dumpIr(objDir, name, c++, ir, ctx.CancellationToken)
                .GetAwaiter()
                .GetResult();
        }
        : (ir, stage) => { });
        Console.WriteLine($"  Done in {Duration.Format(s.Elapsed.Ticks)}");
    }

    // TODO: Re-enable when compilation has been implemented again.
    // var outputDir = Path.GetDirectoryName(path);
    // Console.WriteLine($"Compiling to {outputDir}");
    // await compilation.EmitAsync(syntaxTree, name, new OutputDirectory(outputDir!), optimize, cancellationToken: ctx.CancellationToken)
    //                  .ConfigureAwait(false);
    // Console.WriteLine($"Compiled in {Duration.Format(s.Elapsed.Ticks)}");
    // cilDebugWriter?.Flush();
    // cilDebugWriter?.Dispose();

    return 0;
});

static async Task dumpIr(ObjectFileManager objectFileManager, string name, int num, IrGraph ir, CancellationToken cancellationToken = default)
{
    using var writer = objectFileManager.CreateText(Path.ChangeExtension(name, $"{num}.mir"));

    var indentedWriter = new IndentedTextWriter(writer, "  ");
    await indentedWriter.WriteLineAsync("Edges:");
    indentedWriter.Indent++;
    foreach (var edge in ir.Edges)
        await indentedWriter.WriteLineAsync($"BB{edge.SourceBlockOrdinal} => BB{edge.TargetBlockOrdinal}");
    indentedWriter.Indent--;
    await indentedWriter.WriteLineNoTabsAsync("");

    foreach (var block in ir.BasicBlocks)
    {
        await indentedWriter.WriteAsync("BB");
        await indentedWriter.WriteAsync(block.Ordinal.ToString());
        await indentedWriter.WriteLineAsync(":");

        indentedWriter.Indent++;
        foreach (var instruction in block.Instructions)
            await indentedWriter.WriteLineAsync(instruction.ToRepr());
        indentedWriter.Indent--;
        await indentedWriter.WriteLineNoTabsAsync("");
    }

    await indentedWriter.FlushAsync(cancellationToken)
                        .ConfigureAwait(false);
}
