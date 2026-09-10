using BenchmarkDotNet.Attributes;
using BotForge.Core;

namespace BotForge.Templates.Benchmarks;

/// <summary>
/// Measures inline keyboard building with static buttons (conditional ShowIf),
/// dynamic data with pagination, and varying data set sizes.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class KeyboardBuilderBenchmark
{
    private KeyboardBuilder _builder = null!;
    private MessageTemplate _template = null!;
    private Dictionary<string, string> _parameters = null!;
    private Dictionary<string, IReadOnlyList<KeyboardItemData>> _smallData = null!;
    private Dictionary<string, IReadOnlyList<KeyboardItemData>> _largeData = null!;

    [GlobalSetup]
    public void Setup()
    {
        _builder = BenchmarkFixtures.CreateKeyboardBuilder();
        _template = BenchmarkFixtures.KeyboardTemplate();
        _parameters = new Dictionary<string, string>
        {
            ["IsVip"] = "true",
            ["HasBonus"] = "false"
        };
        _smallData = new Dictionary<string, IReadOnlyList<KeyboardItemData>>
        { ["products"] = BenchmarkFixtures.KeyboardDynamicItems(6) };
        _largeData = new Dictionary<string, IReadOnlyList<KeyboardItemData>>
        { ["products"] = BenchmarkFixtures.KeyboardDynamicItems(100) };
    }

    [Benchmark(Baseline = true)]
    public object? StaticOnly()
    {
        return _builder.BuildInlineKeyboard(_template, _parameters, "en");
    }

    [Benchmark]
    public object? WithSmallDynamic()
    {
        return _builder.BuildInlineKeyboard(_template, _parameters, "en", _smallData);
    }

    [Benchmark]
    public object? WithLargeDynamic()
    {
        return _builder.BuildInlineKeyboard(_template, _parameters, "en", _largeData,
            new Dictionary<string, int> { ["products"] = 5 });
    }
}
