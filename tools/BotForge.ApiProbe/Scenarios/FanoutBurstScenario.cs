using BotForge.ApiProbe.Core;
using Spectre.Console;

namespace BotForge.ApiProbe.Scenarios;

public sealed class FanoutBurstScenario : IProbeScenario
{
    public string Name => "Fanout Burst";

    public string Description =>
        "Send to N different users as fast as possible (round-robin). Tests if >30/s is possible across users.";

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatIds = context.Config.FanoutChatIds;
        if (chatIds.Count == 0)
            throw new InvalidOperationException("FanoutChatIds must have at least one entry.");

        var count = context.Config.Defaults.MessageCount;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;
        var records = new List<RequestRecord>(count);

        var startedAt = DateTimeOffset.UtcNow;

        console.MarkupLine($"[bold]Fanout burst: {count} msgs across {chatIds.Count} user(s), no delay[/]");

        await console.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[bold]Fanout → {chatIds.Count} chats[/]", maxValue: count);

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var chatId = chatIds[i % chatIds.Count];
                    var record = await context.SendProbeMessageAsync(bot, botName, chatId, i, ct);
                    records.Add(record);
                    task.Increment(1);

                    if (record.IsRateLimited)
                        console.MarkupLine(
                            $"  [yellow]429 at #{i} (chat {chatId}) — retry_after={record.RetryAfterSeconds}s[/]");
                }
            });

        var finishedAt = DateTimeOffset.UtcNow;

        // Per-chat breakdown
        var perChat = records.GroupBy(r => r.TargetChatId);
        foreach (var group in perChat)
        {
            var rl = group.Count(r => r.IsRateLimited);
            console.MarkupLine($"  Chat {group.Key}: {group.Count()} sent, {rl} rate-limited");
        }

        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, records);
    }
}
