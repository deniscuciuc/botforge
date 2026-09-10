using System.Globalization;
using System.Text;
using BotForge.ApiProbe.Core;

namespace BotForge.ApiProbe.Reporting;

public sealed class MarkdownReportWriter : IReportWriter
{
    public string Format => "Markdown";

    public async Task WriteAsync(ScenarioResult result, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var fileName = SanitizeFileName(result.ScenarioName);
        var path = Path.Combine(outputDirectory, $"{fileName}_{result.StartedAt:yyyyMMdd_HHmmss}.md");

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"# {result.ScenarioName}");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"> {result.Description}");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"**Run:** {result.StartedAt:yyyy-MM-dd HH:mm:ss} UTC — {result.FinishedAt:HH:mm:ss} UTC ({result.Duration.TotalSeconds:F1}s)");
        sb.AppendLine();

        // Summary table
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Total Requests | {result.TotalRequests} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Succeeded | {result.Succeeded} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Rate Limited (429) | {result.RateLimited} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Other Errors | {result.OtherErrors} |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| Actual RPS | {result.ActualRps:F2} |");

        if (result.TargetRps.HasValue)
            sb.AppendLine(CultureInfo.InvariantCulture, $"| Target RPS | {result.TargetRps.Value:F2} |");

        if (result.ThresholdRps.HasValue)
            sb.AppendLine(CultureInfo.InvariantCulture, $"| **Threshold RPS** | **{result.ThresholdRps.Value:F2}** |");

        sb.AppendLine(CultureInfo.InvariantCulture, $"| Avg Latency | {result.AvgLatencyMs:F1}ms |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| P50 Latency | {result.P50LatencyMs:F1}ms |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| P95 Latency | {result.P95LatencyMs:F1}ms |");
        sb.AppendLine(CultureInfo.InvariantCulture, $"| P99 Latency | {result.P99LatencyMs:F1}ms |");

        if (result.MaxRetryAfterSeconds > 0)
            sb.AppendLine(CultureInfo.InvariantCulture, $"| Max Retry-After | {result.MaxRetryAfterSeconds:F0}s |");

        if (result.TotalPayloadBytes.HasValue)
        {
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"| Total Payload | {FormatBytes(result.TotalPayloadBytes.Value)} |");
            if (result.AvgBytesPerSecond.HasValue)
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"| Avg Bandwidth | {FormatBytes((long)result.AvgBytesPerSecond.Value)}/s |");
        }

        // Per-chat breakdown
        var chatGroups = result.Requests.GroupBy(r => r.TargetChatId).ToList();
        if (chatGroups.Count > 1)
        {
            sb.AppendLine();
            sb.AppendLine("## Per-Chat Breakdown");
            sb.AppendLine();
            sb.AppendLine("| Chat ID | Sent | OK | 429s | Errors |");
            sb.AppendLine("|---------|------|----|------|--------|");

            foreach (var g in chatGroups)
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"| {g.Key} | {g.Count()} | {g.Count(r => !r.IsRateLimited && r.ErrorMessage is null)} | {g.Count(r => r.IsRateLimited)} | {g.Count(r => !r.IsRateLimited && r.ErrorMessage is not null)} |");
        }

        // Per-bot breakdown
        var botGroups = result.Requests.GroupBy(r => r.BotName).ToList();
        if (botGroups.Count > 1)
        {
            sb.AppendLine();
            sb.AppendLine("## Per-Bot Breakdown");
            sb.AppendLine();
            sb.AppendLine("| Bot | Sent | OK | 429s | Errors |");
            sb.AppendLine("|-----|------|----|------|--------|");

            foreach (var g in botGroups)
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"| {g.Key} | {g.Count()} | {g.Count(r => !r.IsRateLimited && r.ErrorMessage is null)} | {g.Count(r => r.IsRateLimited)} | {g.Count(r => !r.IsRateLimited && r.ErrorMessage is not null)} |");
        }

        // First 429s
        var rateLimited = result.Requests.Where(r => r.IsRateLimited).Take(10).ToList();
        if (rateLimited.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"## Rate-Limited Requests (first {rateLimited.Count} of {result.RateLimited})");
            sb.AppendLine();
            sb.AppendLine("| # | Time | Method | Chat | Bot | Retry-After | Latency |");
            sb.AppendLine("|---|------|--------|------|-----|-------------|---------|");

            foreach (var r in rateLimited)
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"| {r.Index} | {r.Timestamp:HH:mm:ss.fff} | {r.ApiMethod} | {r.TargetChatId} | {r.BotName} | {r.RetryAfterSeconds?.ToString() ?? "-"} | {r.Latency.TotalMilliseconds:F0}ms |");
        }

        await File.WriteAllTextAsync(path, sb.ToString());
    }

    private static string SanitizeFileName(string name)
    {
        return string.Concat(name.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Replace(' ', '_');
    }

    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
            >= 1_024 => $"{bytes / 1_024.0:F1} KB",
            _ => $"{bytes} B"
        };
    }
}
