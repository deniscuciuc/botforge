using BotForge.ApiProbe.Core;
using Spectre.Console;

namespace BotForge.ApiProbe.Scenarios;

public sealed class DocumentUploadSustainedScenario : IProbeScenario
{
    public string Name => "Document Upload Sustained";

    public string Description =>
        "Ramp-up rate sending ~100KB documents to private chat until 429s. Finds threshold for document uploads.";

    private const int DocSizeBytes = 100 * 1024; // 100 KB

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatId = context.Config.PrivateChatId;
        if (chatId == 0)
            throw new InvalidOperationException("PrivateChatId must be set in config.");

        var defaults = context.Config.Defaults;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;
        var records = new List<RequestRecord>();

        console.MarkupLine($"[dim]Generating {DocSizeBytes / 1024}KB test document...[/]");
        var docBytes = ProbeTestData.GenerateDocumentBytes(DocSizeBytes);

        var currentRps = defaults.RampStartRps;
        var stepDuration = TimeSpan.FromSeconds(defaults.RampStepDurationSeconds);
        double? thresholdRps = null;
        var index = 0;

        var startedAt = DateTimeOffset.UtcNow;

        console.MarkupLine(
            $"[bold]Ramping 100KB documents from {defaults.RampStartRps} → +{defaults.RampStepRps}/step, step={defaults.RampStepDurationSeconds}s[/]");

        while (!ct.IsCancellationRequested)
        {
            var interval = TimeSpan.FromMilliseconds(1000.0 / currentRps);
            var stepRecords = new List<RequestRecord>();
            var stepStart = DateTimeOffset.UtcNow;

            console.MarkupLine($"  [cyan]Step: {currentRps} doc/s[/]");

            using var timer = new PeriodicTimer(interval);
            while (DateTimeOffset.UtcNow - stepStart < stepDuration)
            {
                ct.ThrowIfCancellationRequested();
                await timer.WaitForNextTickAsync(ct);

                var record = await context.SendDocumentByStreamAsync(bot, botName, chatId, docBytes,
                    $"probe_{index}.bin", DocSizeBytes, index++, ct);
                records.Add(record);
                stepRecords.Add(record);

                if (record.IsRateLimited)
                    console.MarkupLine(
                        $"    [yellow]429 at #{record.Index} — retry_after={record.RetryAfterSeconds}s[/]");
            }

            var rateLimitedPercent = stepRecords.Count > 0
                ? (double)stepRecords.Count(r => r.IsRateLimited) / stepRecords.Count * 100
                : 0;

            console.MarkupLine(
                $"    Sent: {stepRecords.Count}, 429s: {stepRecords.Count(r => r.IsRateLimited)} ({rateLimitedPercent:F1}%)");

            if (rateLimitedPercent > 10)
            {
                thresholdRps = currentRps - defaults.RampStepRps;
                console.MarkupLine(
                    $"  [red]Threshold reached at ~{thresholdRps} doc/s (>10% rate limited at {currentRps} doc/s)[/]");
                break;
            }

            currentRps += defaults.RampStepRps;

            if (currentRps > 100)
            {
                console.MarkupLine("[yellow]Safety cap at 100 doc/s. Stopping ramp.[/]");
                break;
            }
        }

        var finishedAt = DateTimeOffset.UtcNow;
        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, records, currentRps, thresholdRps);
    }
}
