using BenchmarkDotNet.Attributes;

namespace BotForge.Templates.Benchmarks;

/// <summary>
/// Measures parameter substitution scaling. Each parameter triggers a full
/// string.Replace scan — this benchmark detects the O(n×m) quadratic cost
/// where n = parameters and m = template length.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class ParameterSubstitutionBenchmark
{
    private TemplateRenderer _renderer = null!;
    private Dictionary<string, string> _parameters = null!;

    [Params(5, 25, 50, 100)] public int ParamCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var store = new InMemoryMessageTemplateStore();
        store.AddOrReplace(BenchmarkFixtures.ParameterHeavyTemplate(ParamCount));
        _renderer = BenchmarkFixtures.CreateRenderer(store);
        _parameters = BenchmarkFixtures.ParameterHeavyData(ParamCount);
    }

    [Benchmark]
    public string Substitute()
    {
        return _renderer.Render("param_heavy", _parameters, "en");
    }
}
