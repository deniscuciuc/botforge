using BenchmarkDotNet.Attributes;

namespace BotForge.Templates.Benchmarks;

/// <summary>
/// Measures end-to-end render of a simple template with 2 parameter substitutions.
/// Baseline scenario — represents the minimum render cost.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class SimpleRenderBenchmark
{
    private TemplateRenderer _renderer = null!;
    private Dictionary<string, string> _parameters = null!;

    [GlobalSetup]
    public void Setup()
    {
        var store = new InMemoryMessageTemplateStore();
        store.AddOrReplace(BenchmarkFixtures.SimpleTemplate());
        _renderer = BenchmarkFixtures.CreateRenderer(store);
        _parameters = BenchmarkFixtures.SimpleParameters();
    }

    [Benchmark(Baseline = true)]
    public string Render()
    {
        return _renderer.Render("greeting", _parameters, "en");
    }
}
