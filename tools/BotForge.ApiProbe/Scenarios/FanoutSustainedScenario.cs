using BotForge.ApiProbe.Core;
using Spectre.Console;

namespace BotForge.ApiProbe.Scenarios;

public sealed class FanoutSustainedScenario : IProbeScenario
{
    public string Name => "Fanout Sustained";

    public string Description =>
        "Ramp-up rate while distributing across N users. Finds real global per-bot throughput.";

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatIds = context.Config.FanoutChatIds;
        if (chatIds.Count == 0)
            throw new InvalidOperationException("FanoutChatIds must have at least one entry.");

        var defaults = context.Config.Defaults;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;
        var records = new List<RequestRecord>();

        var currentRps = defaults.RampStartRps;
        var stepDuration = TimeSpan.FromSeconds(defaults.RampStepDurationSeconds);
        double? thresholdRps = null;
        var index = 0;

        var startedAt = DateTimeOffset.UtcNow;

        console.MarkupLine(
            $"[bold]Fanout ramp: {chatIds.Count} users, {defaults.RampStartRps} → +{defaults.RampStepRps}/step[/]");

        while (!ct.IsCancellationRequested)
        {
            var interval = TimeSpan.FromMilliseconds(1000.0 / currentRps);
            var stepRecords = new List<RequestRecord>();
            var stepStart = DateTimeOffset.UtcNow;

            console.MarkupLine($"  [cyan]Step: {currentRps} msg/s across {chatIds.Count} users[/]");

            using var timer = new PeriodicTimer(interval);
            while (DateTimeOffset.UtcNow - stepStart < stepDuration)
            {
                ct.ThrowIfCancellationRequested();
                await timer.WaitForNextTickAsync(ct);

                var chatId = chatIds[index % chatIds.Count];
                var record = await context.SendProbeMessageAsync(bot, botName, chatId, index++, ct);
                records.Add(record);
                stepRecords.Add(record);

                if (record.IsRateLimited)
                    console.MarkupLine(
                        $"    [yellow]429 at #{record.Index} (chat {chatId}) — retry_after={record.RetryAfterSeconds}s[/]");
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
                    $"  [red]Threshold at ~{thresholdRps} msg/s (>10% rate limited at {currentRps} msg/s)[/]");
                break;
            }

            currentRps += defaults.RampStepRps;

            // Higher cap for fanout since we're distributing
            if (currentRps > 500)
            {
                console.MarkupLine("[yellow]Safety cap at 500 msg/s for fanout. Stopping ramp.[/]");
                break;
            }
        }

        var finishedAt = DateTimeOffset.UtcNow;

        // Per-chat breakdown
        var perChat = records.GroupBy(r => r.TargetChatId);
        foreach (var group in perChat)
        {
            var rl = group.Count(r => r.IsRateLimited);
            console.MarkupLine($"  Chat {group.Key}: {group.Count()} sent, {rl} rate-limited");
        }

        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, records, currentRps, thresholdRps);
    }
}
