using BotForge.ApiProbe.Core;
using Spectre.Console;

namespace BotForge.ApiProbe.Scenarios;

public sealed class DeleteMessageFanoutBurstScenario : IProbeScenario
{
    public string Name => "Delete Message Fanout Burst";

    public string Description =>
        "Send messages round-robin to fanout users, then burst-delete across all chats. Tests global delete throughput.";

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatIds = context.Config.FanoutChatIds;
        if (chatIds.Count == 0)
            throw new InvalidOperationException("FanoutChatIds must have at least one entry.");

        var count = context.Config.Defaults.MessageCount;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;

        // Phase 1: Send messages round-robin, track (chatId, messageId) pairs
        console.MarkupLine($"[bold]Phase 1: Sending {count} messages across {chatIds.Count} chats...[/]");
        var targets = new List<(long ChatId, int MessageId)>(count);

        await console.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[bold]Sending seed messages (fanout)[/]", maxValue: count);

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var chatId = chatIds[i % chatIds.Count];
                    var record = await context.SendProbeMessageAsync(bot, botName, chatId, i, ct);
                    if (record.MessageId is not null)
                        targets.Add((chatId, record.MessageId.Value));
                    task.Increment(1);
                }
            });

        console.MarkupLine(
            $"[green]{targets.Count} messages prepared. Phase 2: Burst-deleting across {chatIds.Count} chats...[/]");

        // Phase 2: Delete all as fast as possible
        var deleteRecords = new List<RequestRecord>(targets.Count);
        var startedAt = DateTimeOffset.UtcNow;

        await console.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[bold]Delete fanout burst → {targets.Count} msgs[/]", maxValue: targets.Count);

                for (var i = 0; i < targets.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var (chatId, messageId) = targets[i];
                    var record = await context.DeleteProbeMessageAsync(bot, botName, chatId, messageId, i, ct);
                    deleteRecords.Add(record);
                    task.Increment(1);

                    if (record.IsRateLimited)
                        console.MarkupLine(
                            $"  [yellow]429 at delete #{i} (chat {chatId}) — retry_after={record.RetryAfterSeconds}s[/]");
                }
            });

        var finishedAt = DateTimeOffset.UtcNow;

        var perChat = deleteRecords.GroupBy(r => r.TargetChatId);
        foreach (var group in perChat)
        {
            var rl = group.Count(r => r.IsRateLimited);
            console.MarkupLine($"  Chat {group.Key}: {group.Count()} deleted, {rl} rate-limited");
        }

        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, deleteRecords);
    }
}
