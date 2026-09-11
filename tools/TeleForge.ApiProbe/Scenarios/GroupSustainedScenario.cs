using Spectre.Console;
using TeleForge.ApiProbe.Core;

namespace TeleForge.ApiProbe.Scenarios;

public sealed class GroupSustainedScenario : IProbeScenario
{
    public string Name => "Group Sustained";
    public string Description => "Ramp-up rate to a group chat until 429s appear. Finds exact group threshold.";

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatId = context.Config.GroupChatId;
        if (chatId == 0)
            throw new InvalidOperationException("GroupChatId must be set in config.");

        var defaults = context.Config.Defaults;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;
        var records = new List<RequestRecord>();

        // For groups, we ramp in messages-per-minute since the limit is 20/min
        var currentMpm = 5; // start at 5 msgs/min
        var stepMpm = 5; // +5 msgs/min each step
        double? thresholdMpm = null;
        var index = 0;
        var stepDuration = TimeSpan.FromSeconds(defaults.RampStepDurationSeconds * 2); // longer steps for group timing

        var startedAt = DateTimeOffset.UtcNow;

        console.MarkupLine($"[bold]Group ramp: starting at {currentMpm} msg/min, +{stepMpm}/step[/]");

        while (!ct.IsCancellationRequested)
        {
            var intervalMs = 60_000.0 / currentMpm;
            var interval = TimeSpan.FromMilliseconds(intervalMs);
            var stepRecords = new List<RequestRecord>();
            var stepStart = DateTimeOffset.UtcNow;

            console.MarkupLine($"  [cyan]Step: {currentMpm} msg/min ({intervalMs:F0}ms interval)[/]");

            using var timer = new PeriodicTimer(interval);
            while (DateTimeOffset.UtcNow - stepStart < stepDuration)
            {
                ct.ThrowIfCancellationRequested();
                await timer.WaitForNextTickAsync(ct);

                var record = await context.SendProbeMessageAsync(bot, botName, chatId, index++, ct);
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
                thresholdMpm = currentMpm - stepMpm;
                console.MarkupLine(
                    $"  [red]Threshold at ~{thresholdMpm} msg/min (>10% rate limited at {currentMpm} msg/min)[/]");
                break;
            }

            currentMpm += stepMpm;

            if (currentMpm > 60)
            {
                console.MarkupLine("[yellow]Safety cap at 60 msg/min for group. Stopping ramp.[/]");
                break;
            }
        }

        var finishedAt = DateTimeOffset.UtcNow;
        var targetRps = currentMpm / 60.0;
        var thresholdRps = thresholdMpm.HasValue ? thresholdMpm.Value / 60.0 : (double?)null;
        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, records, targetRps, thresholdRps);
    }
}
