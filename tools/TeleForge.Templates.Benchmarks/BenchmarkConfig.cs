using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

namespace TeleForge.Templates.Benchmarks;

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(3)
            .WithIterationCount(15)
            .WithId("Default"));

        AddDiagnoser(MemoryDiagnoser.Default);

        // Markdown, HTML, CSV are added by default — only add JSON Full
        AddExporter(JsonExporter.Full);

        AddColumn(StatisticColumn.P95);
        AddColumn(StatisticColumn.Iterations);

        WithSummaryStyle(SummaryStyle.Default
            .WithRatioStyle(RatioStyle.Trend));

        WithArtifactsPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "BenchmarkArtifacts"));
    }
}
