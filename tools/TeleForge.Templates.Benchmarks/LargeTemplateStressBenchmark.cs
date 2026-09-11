using BenchmarkDotNet.Attributes;

namespace TeleForge.Templates.Benchmarks;

/// <summary>
/// Stress test: large template with 20 conditional sections, formatters,
/// and a 50-item loop. Exercises the full 8-stage rendering pipeline
/// under heavy load.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class LargeTemplateStressBenchmark
{
    private TemplateRenderer _renderer = null!;
    private Dictionary<string, string> _parameters = null!;
    private IDictionary<string, IReadOnlyList<IDictionary<string, string>>> _loopData = null!;

    [GlobalSetup]
    public void Setup()
    {
        var store = new InMemoryMessageTemplateStore();
        store.AddOrReplace(BenchmarkFixtures.LargeTemplate());
        _renderer = BenchmarkFixtures.CreateRenderer(store);
        _parameters = BenchmarkFixtures.LargeTemplateParameters();
        _loopData = BenchmarkFixtures.LargeTemplateLoopData();
    }

    [Benchmark]
    public string RenderLarge()
    {
        return _renderer.Render("large_stress", _parameters, "en", _loopData);
    }
}
