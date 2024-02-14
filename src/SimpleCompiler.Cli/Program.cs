using System.CodeDom.Compiler;
using System.Diagnostics;
using Cocona;
using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Text;
using SimpleCompiler.Backends.Cil;
using SimpleCompiler.Cli;
using SimpleCompiler.Cli.Validation;
using SimpleCompiler.Compiler;
using SimpleCompiler.FileSystem;
using SimpleCompiler.Frontends.Lua;
using SimpleCompiler.IR;
using Tsu.Numerics;

await CoconaLiteApp.RunAsync(async (
    [Argument][FileExists] string path,
    [Option('d')] bool debug,
    [Option('O')] bool optimize,
    CoconaAppContext ctx) =>
{
    SourceText sourceText;
    using (var stream = File.OpenRead(path))
        sourceText = SourceText.From(stream, throwIfBinaryDetected: true);

    var s = Stopwatch.StartNew();
    var syntaxTree = LuaSyntaxTree.ParseText(sourceText, new LuaParseOptions(LuaSyntaxOptions.Lua51), path, ctx.CancellationToken);
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
    var ir = compilation.GetTree(syntaxTree);
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
        ir = compilation.GetOptimizedTree(syntaxTree, debug ? (ir, stage) =>
        {
            Console.WriteLine($"  {c}: {stage}");

            dumpIr(objDir, name, c++, ir, ctx.CancellationToken)
                .GetAwaiter()
                .GetResult();
        }
        : (ir, stage) => { });
        Console.WriteLine($"  Done in {Duration.Format(s.Elapsed.Ticks)}");

        if (debug)
        {
            await dumpIr(objDir, name, c++, ir, ctx.CancellationToken);
        }
    }

    // TODO: Re-enable when compilation has been implemented.
    // var outputDir = Path.GetDirectoryName(path);
    // Console.WriteLine($"Compiling to {outputDir}");
    // await compilation.EmitAsync(syntaxTree, name, new OutputDirectory(outputDir!), optimize, cancellationToken: ctx.CancellationToken)
    //                  .ConfigureAwait(false);
    // Console.WriteLine($"Compiled in {Duration.Format(s.Elapsed.Ticks)}");
    // cilDebugWriter?.Flush();
    // cilDebugWriter?.Dispose();

    return 0;
});

static async Task dumpIr(ObjectFileManager objectFileManager, string name, int num, ComputedIr ir, CancellationToken cancellationToken = default)
{
    using var writer = objectFileManager.CreateText(Path.ChangeExtension(name, $"{num}.mir"));

    var indentedWriter = new IndentedTextWriter(writer, "  ");
    foreach (var block in ir.BasicBlocks)
    {
        await indentedWriter.WriteAsync("BB");
        await indentedWriter.WriteAsync(block.BlockId.ToString());
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
