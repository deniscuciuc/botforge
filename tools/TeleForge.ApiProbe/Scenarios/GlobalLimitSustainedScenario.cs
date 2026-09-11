using Spectre.Console;
using TeleForge.ApiProbe.Core;

namespace TeleForge.ApiProbe.Scenarios;

public sealed class GlobalLimitSustainedScenario : IProbeScenario
{
    public string Name => "Global Limit Sustained";

    public string Description =>
        "Ramp total throughput distributed across ALL configured chats. Finds the absolute sustainable per-bot RPS.";

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var config = context.Config;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;

        // Collect all configured chat IDs
        var chatTargets = new List<(long ChatId, string Label)>();

        if (config.PrivateChatId != 0)
            chatTargets.Add((config.PrivateChatId, "private"));
        if (config.GroupChatId != 0)
            chatTargets.Add((config.GroupChatId, "group"));
        if (config.SupergroupChatId != 0)
            chatTargets.Add((config.SupergroupChatId, "supergroup"));
        if (config.ChannelChatId != 0)
            chatTargets.Add((config.ChannelChatId, "channel"));
        foreach (var fanoutId in config.FanoutChatIds)
            if (!chatTargets.Any(t => t.ChatId == fanoutId))
                chatTargets.Add((fanoutId, "fanout"));

        if (chatTargets.Count == 0)
            throw new InvalidOperationException("No chat IDs configured.");

        var defaults = config.Defaults;
        var records = new List<RequestRecord>();

        var currentRps = defaults.RampStartRps;
        var stepDuration = TimeSpan.FromSeconds(defaults.RampStepDurationSeconds);
        double? thresholdRps = null;
        var index = 0;

        var startedAt = DateTimeOffset.UtcNow;

        console.MarkupLine(
            $"[bold]Global ramp: {chatTargets.Count} chats, {defaults.RampStartRps} → +{defaults.RampStepRps}/step[/]");
        foreach (var (chatId, label) in chatTargets)
            console.MarkupLine($"  {label}: {chatId}");

        while (!ct.IsCancellationRequested)
        {
            var interval = TimeSpan.FromMilliseconds(1000.0 / currentRps);
            var stepRecords = new List<RequestRecord>();
            var stepStart = DateTimeOffset.UtcNow;

            console.MarkupLine($"  [cyan]Step: {currentRps} msg/s across {chatTargets.Count} chats[/]");

            using var timer = new PeriodicTimer(interval);
            while (DateTimeOffset.UtcNow - stepStart < stepDuration)
            {
                ct.ThrowIfCancellationRequested();
                await timer.WaitForNextTickAsync(ct);

                var chatTarget = chatTargets[index % chatTargets.Count];
                var record = await context.SendProbeMessageAsync(bot, botName, chatTarget.ChatId, index++, ct);
                records.Add(record);
                stepRecords.Add(record);

                if (record.IsRateLimited)
                    console.MarkupLine(
                        $"    [yellow]429 at #{record.Index} ({chatTarget.Label} {chatTarget.ChatId}) — retry_after={record.RetryAfterSeconds}s[/]");
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
                    $"  [red]Global threshold at ~{thresholdRps} msg/s (>10% rate limited at {currentRps} msg/s)[/]");
                break;
            }

            currentRps += defaults.RampStepRps;

            if (currentRps > 500)
            {
                console.MarkupLine("[yellow]Safety cap at 500 msg/s for global test. Stopping ramp.[/]");
                break;
            }
        }

        var finishedAt = DateTimeOffset.UtcNow;

        // Per-chat-type breakdown
        console.MarkupLine("[bold]Per-chat-type breakdown:[/]");
        var perChat = records.GroupBy(r => r.TargetChatId);
        foreach (var group in perChat)
        {
            var label = chatTargets.FirstOrDefault(t => t.ChatId == group.Key).Label ?? "unknown";
            var rl = group.Count(r => r.IsRateLimited);
            console.MarkupLine($"  {label} ({group.Key}): {group.Count()} sent, {rl} rate-limited");
        }

        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, records, currentRps, thresholdRps);
    }
}
