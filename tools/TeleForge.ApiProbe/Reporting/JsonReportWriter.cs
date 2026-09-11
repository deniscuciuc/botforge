using System.Text.Json;
using System.Text.Json.Serialization;
using TeleForge.ApiProbe.Core;

namespace TeleForge.ApiProbe.Reporting;

public sealed class JsonReportWriter : IReportWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public string Format => "JSON";

    public async Task WriteAsync(ScenarioResult result, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var fileName = SanitizeFileName(result.ScenarioName);
        var path = Path.Combine(outputDirectory, $"{fileName}_{result.StartedAt:yyyyMMdd_HHmmss}.json");

        var json = JsonSerializer.Serialize(result, SerializerOptions);
        await File.WriteAllTextAsync(path, json);
    }

    private static string SanitizeFileName(string name)
    {
        return string.Concat(name.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Replace(' ', '_');
    }
}
