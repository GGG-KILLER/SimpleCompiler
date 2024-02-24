using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Loretta.CodeAnalysis.Lua;
using SimpleCompiler.Frontends.Lua;
using SimpleCompiler.IR;

namespace SimpleCompiler.Benchmarks;

[DryJob(RuntimeMoniker.NativeAot80)]
[SimpleJob(RuntimeMoniker.NativeAot80)]
[DryJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net80)]
public class SsaBenchmark
{
    private IrGraph _refGraph = null!;

    [GlobalSetup]
    public void Setup()
    {
        var syntaxTree = LuaSyntaxTree.ParseText("""
            local a, b = 1
            if a % 2 == 0 then
                a = a + 2
                b = 2
            elseif a % 3 == 0 then
                a = a + 3
            elseif a % 4 == 0 then
                a = a + 4
                b = 4
            elseif a % 5 == 0 then
                a = a + 5
            elseif a % 6 == 0 then
                a = a + 6
                b = 6
            elseif a % 7 == 0 then
                a = a + 7
            elseif a % 8 == 0 then
                a = a + 8
                b = 8
            elseif a % 9 == 0 then
                a = a + 9
            else
                a = a + 10
                b = 10
            end
            print(a, b)
        """);
        _refGraph = LuaFrontend.LowerWithoutSsa(syntaxTree);
    }

    [Benchmark(Baseline = true)]
    public IrGraph Rewrite()
    {
        var graph = _refGraph.Clone();
        SsaRewriter.RewriteGraph(graph);
        return graph;
    }
}
