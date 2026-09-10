using BenchmarkDotNet.Attributes;

namespace BotForge.Templates.Benchmarks;

/// <summary>
/// Measures the formatter pipeline with all 9 built-in formatters exercised
/// in a single template render. Stresses regex matching and formatter dispatch.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class FormatterPipelineBenchmark
{
    private TemplateRenderer _renderer = null!;
    private Dictionary<string, string> _parameters = null!;

    [GlobalSetup]
    public void Setup()
    {
        var store = new InMemoryMessageTemplateStore();
        store.AddOrReplace(BenchmarkFixtures.FormatterTemplate());
        _renderer = BenchmarkFixtures.CreateRenderer(store);
        _parameters = BenchmarkFixtures.FormatterParameters();
    }

    [Benchmark]
    public string AllFormatters()
    {
        return _renderer.Render("stats", _parameters, "en");
    }
}
