// See https://aka.ms/new-console-template for more information

using System.CodeDom.Compiler;
using Cocona;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Text;
using SimpleCompiler;
using SimpleCompiler.Cli.Validation;
using SimpleCompiler.MIR;

await CoconaLiteApp.RunAsync(async ([Argument][FileExists] string path, CoconaAppContext ctx) =>
{
    SourceText sourceText;
    using (var stream = File.OpenRead(path))
        sourceText = SourceText.From(stream, throwIfBinaryDetected: true);

    var tree = LuaSyntaxTree.ParseText(sourceText, new LuaParseOptions(LuaSyntaxOptions.Lua51), path, ctx.CancellationToken);

    if (tree.GetDiagnostics().Any())
    {
        foreach (var diagnostic in tree.GetDiagnostics())
        {
            Console.Error.WriteLine(diagnostic.ToString());
        }
        return 1;
    }

    var globalScope = new ScopeInfo(SimpleCompiler.ScopeKind.Global, null);
    var syntaxLowerer = new SyntaxLowerer(globalScope);
    var mirRoot = syntaxLowerer.Visit(await tree.GetRootAsync(ctx.CancellationToken))!;

    await Console.Out.FlushAsync(ctx.CancellationToken);

    var indentedWriter = new IndentedTextWriter(Console.Out);
    var debugWriter = new MirDebugPrinter(indentedWriter);
    indentedWriter.Write("Global Scope: ");
    debugWriter.WriteScope(globalScope);
    indentedWriter.WriteLine();
    debugWriter.Visit(mirRoot);
    await indentedWriter.FlushAsync(ctx.CancellationToken);

    return 0;
});