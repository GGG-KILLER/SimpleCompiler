using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua.Experimental;
using SimpleCompiler.Emit;
using SimpleCompiler.LIR;
using SimpleCompiler.MIR;

namespace SimpleCompiler.Compiler;

public sealed class Compilation
{
    private readonly SyntaxTree _syntaxTree;
    private MirNode? _mirRoot;
    private MirNode? _optimizedMirRoot;
    private IReadOnlyList<Instruction>? _lir;

    public ScopeInfo GlobalScope { get; }

    public Compilation(SyntaxTree syntaxTree)
    {
        GlobalScope = new ScopeInfo(MIR.ScopeKind.Global, null);
        _syntaxTree = syntaxTree;
    }

    public MirNode LowerSyntax()
    {
        if (_mirRoot is null)
        {
            var folded = _syntaxTree.GetRoot().ConstantFold(ConstantFoldingOptions.All);
            var lowerer = new SyntaxLowerer(GlobalScope);
            Interlocked.CompareExchange(ref _mirRoot, lowerer.Visit(folded)!, null);
        }
        return _mirRoot;
    }

    public MirNode OptimizeLoweredSyntax()
    {
        if (_optimizedMirRoot is null)
        {
            var node = LowerSyntax();
            node = ConstantFolder.ConstantFold(node);
            Interlocked.CompareExchange(ref _optimizedMirRoot, node, null);
        }
        return _optimizedMirRoot;
    }

    public IEnumerable<Instruction> LowerMir()
    {
        if (_lir is null)
        {
            var node = OptimizeLoweredSyntax();
            Interlocked.CompareExchange(ref _lir, MirLowerer.Lower(node), null);
        }
        return _lir;
    }

    public async Task EmitAsync(string name, Stream stream, TextWriter? cilDebugWriter = null) =>
        await Emitter.EmitAsync(name, GlobalScope.KnownGlobals, stream, LowerMir(), cilDebugWriter)
                     .ConfigureAwait(false);
}
