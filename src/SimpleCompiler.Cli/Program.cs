// See https://aka.ms/new-console-template for more information

using System.CodeDom.Compiler;
using System.Reflection;
using Cocona;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Text;
using SimpleCompiler.Cli.Validation;
using SimpleCompiler.Compiler;
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

    var name = Path.GetFileNameWithoutExtension(path);
    var compiler = Compiler.Create(new AssemblyName(name));

    var mirRoot = compiler.LowerSyntax((LuaSyntaxTree)tree);

    await Console.Out.FlushAsync(ctx.CancellationToken);

    var indentedWriter = new IndentedTextWriter(Console.Out, "    ");
    indentedWriter.Indent++;
    indentedWriter.WriteLine("MIR:");
    var debugWriter = new MirDebugPrinter(indentedWriter);
    indentedWriter.Write("Global Scope: ");
    debugWriter.WriteScope(compiler.GlobalScope);
    indentedWriter.WriteLine();
    debugWriter.Visit(mirRoot);
    await indentedWriter.FlushAsync(ctx.CancellationToken);

    Console.WriteLine();
    Console.WriteLine("LIR:");
    var instrs = compiler.LowerMir(mirRoot);
    foreach (var instr in instrs)
    {
        Console.Write("    ");
        Console.WriteLine(instr.ToRepr());
    }

    Console.WriteLine(value: "Compiling...");
    var (_, m) = compiler.CompileProgram(instrs);

    m.Invoke(null, null);

    using (var stream = File.Open(Path.ChangeExtension(path, ".dll"), FileMode.Create, FileAccess.Write))
    {
        await compiler.SaveAsync(stream);
    }

    return 0;
});