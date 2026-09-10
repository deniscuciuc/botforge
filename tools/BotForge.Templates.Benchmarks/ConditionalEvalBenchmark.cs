using BenchmarkDotNet.Attributes;

namespace BotForge.Templates.Benchmarks;

/// <summary>
/// Measures conditional evaluation including nested if/else blocks,
/// comparison operators, and logical combinators (&&, ||).
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class ConditionalEvalBenchmark
{
    private TemplateRenderer _renderer = null!;
    private Dictionary<string, string> _simpleParams = null!;
    private Dictionary<string, string> _nestedParams = null!;

    [GlobalSetup]
    public void Setup()
    {
        var store = new InMemoryMessageTemplateStore();
        store.AddOrReplace(BenchmarkFixtures.ConditionalTemplate());
        store.AddOrReplace(BenchmarkFixtures.NestedConditionalTemplate());
        _renderer = BenchmarkFixtures.CreateRenderer(store);
        _simpleParams = BenchmarkFixtures.ConditionalParameters();
        _nestedParams = BenchmarkFixtures.NestedConditionalParameters();
    }

    [Benchmark(Baseline = true)]
    public string SimpleConditionals()
    {
        return _renderer.Render("status", _simpleParams, "en");
    }

    [Benchmark]
    public string NestedConditionals()
    {
        return _renderer.Render("nested_cond", _nestedParams, "en");
    }
}
