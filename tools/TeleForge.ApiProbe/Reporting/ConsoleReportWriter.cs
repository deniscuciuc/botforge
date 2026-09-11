using Spectre.Console;
using TeleForge.ApiProbe.Core;

namespace TeleForge.ApiProbe.Reporting;

public sealed class ConsoleReportWriter(IAnsiConsole console) : IReportWriter
{
    public string Format => "Console";

    public Task WriteAsync(ScenarioResult result, string outputDirectory)
    {
        console.WriteLine();
        console.Write(new Rule($"[bold yellow]{result.ScenarioName}[/]").RuleStyle("grey"));
        console.MarkupLine($"[dim]{result.Description}[/]");
        console.WriteLine();

        var summaryTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Metric")
            .AddColumn("Value");

        summaryTable.AddRow("Duration", $"{result.Duration.TotalSeconds:F1}s");
        summaryTable.AddRow("Total Requests", result.TotalRequests.ToString());
        summaryTable.AddRow("[green]Succeeded[/]", $"[green]{result.Succeeded}[/]");
        summaryTable.AddRow("[yellow]Rate Limited (429)[/]", $"[yellow]{result.RateLimited}[/]");
        summaryTable.AddRow("[red]Other Errors[/]", $"[red]{result.OtherErrors}[/]");
        summaryTable.AddRow("Actual RPS", $"{result.ActualRps:F2}");

        if (result.TargetRps.HasValue)
            summaryTable.AddRow("Target RPS", $"{result.TargetRps.Value:F2}");

        if (result.ThresholdRps.HasValue)
            summaryTable.AddRow("[bold]Threshold RPS[/]", $"[bold]{result.ThresholdRps.Value:F2}[/]");

        summaryTable.AddRow("Avg Latency", $"{result.AvgLatencyMs:F1}ms");
        summaryTable.AddRow("P50 Latency", $"{result.P50LatencyMs:F1}ms");
        summaryTable.AddRow("P95 Latency", $"{result.P95LatencyMs:F1}ms");
        summaryTable.AddRow("P99 Latency", $"{result.P99LatencyMs:F1}ms");

        if (result.MaxRetryAfterSeconds > 0)
            summaryTable.AddRow("Max Retry-After", $"{result.MaxRetryAfterSeconds:F0}s");

        if (result.TotalPayloadBytes.HasValue)
        {
            summaryTable.AddRow("Total Payload", FormatBytes(result.TotalPayloadBytes.Value));
            if (result.AvgBytesPerSecond.HasValue)
                summaryTable.AddRow("Avg Bandwidth", $"{FormatBytes((long)result.AvgBytesPerSecond.Value)}/s");
        }

        console.Write(summaryTable);

        // Rate-limited requests detail (show first 20)
        var rateLimited = result.Requests.Where(r => r.IsRateLimited).Take(20).ToList();
        if (rateLimited.Count > 0)
        {
            console.WriteLine();
            console.MarkupLine(
                $"[yellow]Rate-limited requests (showing {rateLimited.Count} of {result.RateLimited}):[/]");

            var rlTable = new Table()
                .Border(TableBorder.Simple)
                .AddColumn("#")
                .AddColumn("Time")
                .AddColumn("Method")
                .AddColumn("Chat")
                .AddColumn("Bot")
                .AddColumn("Retry-After")
                .AddColumn("Latency");

            foreach (var r in rateLimited)
                rlTable.AddRow(
                    r.Index.ToString(),
                    r.Timestamp.ToString("HH:mm:ss.fff"),
                    r.ApiMethod.ToString(),
                    r.TargetChatId.ToString(),
                    r.BotName,
                    r.RetryAfterSeconds?.ToString() ?? "-",
                    $"{r.Latency.TotalMilliseconds:F0}ms");

            console.Write(rlTable);
        }

        // Per-chat breakdown if multiple chats
        var chatGroups = result.Requests.GroupBy(r => r.TargetChatId).ToList();
        if (chatGroups.Count > 1)
        {
            console.WriteLine();
            console.MarkupLine("[bold]Per-chat breakdown:[/]");

            var chatTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Chat ID")
                .AddColumn("Sent")
                .AddColumn("OK")
                .AddColumn("429s")
                .AddColumn("Errors");

            foreach (var g in chatGroups)
                chatTable.AddRow(
                    g.Key.ToString(),
                    g.Count().ToString(),
                    g.Count(r => !r.IsRateLimited && r.ErrorMessage is null).ToString(),
                    g.Count(r => r.IsRateLimited).ToString(),
                    g.Count(r => !r.IsRateLimited && r.ErrorMessage is not null).ToString());

            console.Write(chatTable);
        }

        // Per-bot breakdown if multiple bots
        var botGroups = result.Requests.GroupBy(r => r.BotName).ToList();
        if (botGroups.Count > 1)
        {
            console.WriteLine();
            console.MarkupLine("[bold]Per-bot breakdown:[/]");

            var botTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Bot")
                .AddColumn("Sent")
                .AddColumn("OK")
                .AddColumn("429s")
                .AddColumn("Errors");

            foreach (var g in botGroups)
                botTable.AddRow(
                    g.Key,
                    g.Count().ToString(),
                    g.Count(r => !r.IsRateLimited && r.ErrorMessage is null).ToString(),
                    g.Count(r => r.IsRateLimited).ToString(),
                    g.Count(r => !r.IsRateLimited && r.ErrorMessage is not null).ToString());

            console.Write(botTable);
        }

        console.Write(new Rule().RuleStyle("grey"));
        return Task.CompletedTask;
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
