// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(
    args,
    DefaultConfig.Instance
        .HideColumns("Runtime", "RunStrategy", "IterationCount", "LaunchCount", "UnrollFactor", "WarmupCount")
        .AddExporter(MarkdownExporter.GitHub)
        .AddColumn(StatisticColumn.Min, StatisticColumn.Mean, StatisticColumn.Mean, StatisticColumn.P95, StatisticColumn.Max));
