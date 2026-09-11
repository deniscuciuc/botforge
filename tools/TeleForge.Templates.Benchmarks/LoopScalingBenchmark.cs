using BenchmarkDotNet.Attributes;

namespace TeleForge.Templates.Benchmarks;

/// <summary>
/// Measures loop expansion scaling across different item counts.
/// Key scenario for detecting O(n) vs O(n²) behavior in LoopProcessor.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class LoopScalingBenchmark
{
    private TemplateRenderer _renderer = null!;
    private Dictionary<string, string> _parameters = null!;
    private IDictionary<string, IReadOnlyList<IDictionary<string, string>>> _loopData = null!;

    [Params(3, 10, 100, 1000)] public int ItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var store = new InMemoryMessageTemplateStore();
        store.AddOrReplace(BenchmarkFixtures.LoopTemplate());
        _renderer = BenchmarkFixtures.CreateRenderer(store);
        _parameters = [];
        _loopData = BenchmarkFixtures.LoopData(ItemCount);
    }

    [Benchmark]
    public string RenderLoop()
    {
        return _renderer.Render("leaderboard", _parameters, "en", _loopData);
    }
}
