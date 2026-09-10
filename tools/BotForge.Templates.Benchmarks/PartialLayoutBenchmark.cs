using BenchmarkDotNet.Attributes;

namespace BotForge.Templates.Benchmarks;

/// <summary>
/// Measures partial resolution and layout inheritance. Exercises the PartialResolver
/// recursive include path and layout block replacement.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class PartialLayoutBenchmark
{
    private TemplateRenderer _renderer = null!;
    private Dictionary<string, string> _parameters = null!;

    [GlobalSetup]
    public void Setup()
    {
        var store = BenchmarkFixtures.PartialStore();
        var emojis = BenchmarkFixtures.CreateEmojiRegistry();
        _renderer = BenchmarkFixtures.CreateRenderer(store, emojis);
        _parameters = new Dictionary<string, string>
        {
            ["Title"] = "Dashboard",
            ["UserName"] = "Alex",
            ["BotName"] = "TestBot"
        };
    }

    [Benchmark]
    public string RenderWithLayout()
    {
        return _renderer.Render("page_with_layout", _parameters, "en");
    }
}
