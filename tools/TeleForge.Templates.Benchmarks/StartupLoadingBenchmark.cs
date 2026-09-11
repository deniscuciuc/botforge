using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;

namespace TeleForge.Templates.Benchmarks;

/// <summary>
/// Measures YAML template loading and deserialization. This is a startup-cost
/// benchmark — typically run once during application boot.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class StartupLoadingBenchmark
{
    private string _tempDir = null!;

    [Params(10, 50, 200)] public int TemplateCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"bench_templates_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var yaml = BenchmarkFixtures.GenerateYamlTemplates(TemplateCount);
        File.WriteAllText(Path.Combine(_tempDir, "templates.yml"), yaml);
    }

    [Benchmark]
    public int LoadFromDirectory()
    {
        var store = new InMemoryMessageTemplateStore();
        var emojis = new EmojiRegistry();
        var loader = new YamlTemplateLoader(store, emojis, NullLogger<YamlTemplateLoader>.Instance);
        loader.LoadFromDirectory(_tempDir);
        return store.GetAll().Count;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
