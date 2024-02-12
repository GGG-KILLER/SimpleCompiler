using System.CodeDom.Compiler;
using System.Diagnostics;
using Cocona;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Text;
using SimpleCompiler.Backends.Cil;
using SimpleCompiler.Cli;
using SimpleCompiler.Cli.Validation;
using SimpleCompiler.Compiler;
using SimpleCompiler.FileSystem;
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

    TextWriter? cilDebugWriter = null;
    if (debug)
        cilDebugWriter = objDir.CreateText(name + ".cil");
    var backend = new CilBackend(cilDebugWriter);

    var compilation = new Compilation(syntaxTree, backend);

    s.Restart();
    var mirTree = compilation.GetTree();
    Console.WriteLine($"Syntax lowering done in {Duration.Format(s.Elapsed.Ticks)}");

    if (debug)
    {
        s.Restart();
        mirTree.Ssa.Compute();
        Console.WriteLine($"  SSA computation done in {Duration.Format(s.Elapsed.Ticks)}");

        await dumpIr(objDir, name, 1, mirTree, ctx.CancellationToken);
    }

    var c = 2;
    if (optimize)
    {
        s.Restart();
        Console.WriteLine($"Optimizing...");
        mirTree = compilation.GetOptimizedTree(debug ? (root, stage) =>
        {
            Console.WriteLine($"  {c}: {stage}");

            dumpIr(objDir, name, c++, new IrTree(mirTree.GlobalScope, root), ctx.CancellationToken)
                .GetAwaiter()
                .GetResult();
        }
        : null);
        Console.WriteLine($"  Done in {Duration.Format(s.Elapsed.Ticks)}");

        if (debug)
        {
            s.Restart();
            mirTree.Ssa.Compute();
            Console.WriteLine($"  SSA computation done in {Duration.Format(s.Elapsed.Ticks)}");

            await dumpIr(objDir, name, c++, mirTree, ctx.CancellationToken);
        }
    }

    if (debug)
    {
        using var textWriter = objDir.CreateText(Path.ChangeExtension(path, $"mir.lua"));
        IrLifter.Lift(mirTree.Root).WriteTo(textWriter);
    }

    var outputDir = Path.GetDirectoryName(path);
    Console.WriteLine($"Compiling to {outputDir}");
    await compilation.EmitAsync(name, new OutputDirectory(outputDir!), optimize, cancellationToken: ctx.CancellationToken)
                     .ConfigureAwait(false);
    Console.WriteLine($"Compiled in {Duration.Format(s.Elapsed.Ticks)}");
    cilDebugWriter?.Flush();
    cilDebugWriter?.Dispose();

    return 0;
});

static async Task dumpIr(ObjectFileManager objectFileManager, string name, int num, IrTree tree, CancellationToken cancellationToken = default)
{
    using var writer = objectFileManager.CreateText(Path.ChangeExtension(name, $"{num}.mir"));

    var indentedWriter = new IndentedTextWriter(writer, "    ");
    var debugWriter = new IrDebugPrinter(indentedWriter, tree.Ssa);
    indentedWriter.Write("Global Scope: ");
    debugWriter.WriteScope(tree.GlobalScope);
    indentedWriter.WriteLine();
    debugWriter.Visit(tree.Root);

    await indentedWriter.FlushAsync(cancellationToken)
                        .ConfigureAwait(false);
}
