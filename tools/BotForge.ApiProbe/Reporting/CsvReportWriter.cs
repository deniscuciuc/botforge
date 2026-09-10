using System.Globalization;
using System.Text;
using BotForge.ApiProbe.Core;

namespace BotForge.ApiProbe.Reporting;

public sealed class CsvReportWriter : IReportWriter
{
    public string Format => "CSV";

    public async Task WriteAsync(ScenarioResult result, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var fileName = SanitizeFileName(result.ScenarioName);
        var path = Path.Combine(outputDirectory, $"{fileName}_{result.StartedAt:yyyyMMdd_HHmmss}.csv");

        var sb = new StringBuilder();
        sb.AppendLine(
            "Index,Timestamp,LatencyMs,HttpStatus,IsRateLimited,RetryAfterSeconds,TargetChatId,BotName,MessageId,ApiMethod,PayloadBytes,ErrorMessage");

        foreach (var r in result.Requests)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{r.Index},");
            sb.Append(CultureInfo.InvariantCulture, $"{r.Timestamp:O},");
            sb.Append(CultureInfo.InvariantCulture, $"{r.Latency.TotalMilliseconds:F2},");
            sb.Append(CultureInfo.InvariantCulture, $"{(int)r.HttpStatus},");
            sb.Append(CultureInfo.InvariantCulture, $"{r.IsRateLimited},");
            sb.Append(CultureInfo.InvariantCulture, $"{r.RetryAfterSeconds?.ToString() ?? ""},");
            sb.Append(CultureInfo.InvariantCulture, $"{r.TargetChatId},");
            sb.Append(CultureInfo.InvariantCulture, $"{r.BotName},");
            sb.Append(CultureInfo.InvariantCulture, $"{r.MessageId?.ToString() ?? ""},");
            sb.Append(CultureInfo.InvariantCulture, $"{r.ApiMethod},");
            sb.Append(CultureInfo.InvariantCulture, $"{r.PayloadBytes?.ToString() ?? ""},");
            sb.AppendLine(CsvEscape(r.ErrorMessage ?? ""));
        }

        await File.WriteAllTextAsync(path, sb.ToString());
    }

    private static string SanitizeFileName(string name)
    {
        return string.Concat(name.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Replace(' ', '_');
    }

    private static string CsvEscape(string value)
    {
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
