using Spectre.Console;
using TeleForge.ApiProbe.Core;

namespace TeleForge.ApiProbe.Scenarios;

public sealed class DeleteMessageBurstScenario : IProbeScenario
{
    public string Name => "Delete Message Burst (Private)";

    public string Description =>
        "Send N messages, then burst-delete all. Finds the hard delete rate limit per private chat.";

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatId = context.Config.PrivateChatId;
        if (chatId == 0)
            throw new InvalidOperationException("PrivateChatId must be set in config.");

        var count = context.Config.Defaults.MessageCount;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;

        // Phase 1: Send N messages to collect message IDs
        console.MarkupLine($"[bold]Phase 1: Sending {count} messages to prepare for deletion...[/]");
        var messageIds = new List<int>(count);

        await console.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[bold]Sending seed messages[/]", maxValue: count);

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var record = await context.SendProbeMessageAsync(bot, botName, chatId, i, ct);
                    if (record.MessageId is not null)
                        messageIds.Add(record.MessageId.Value);
                    task.Increment(1);
                }
            });

        console.MarkupLine($"[green]{messageIds.Count} messages prepared. Phase 2: Burst-deleting...[/]");

        // Phase 2: Delete all as fast as possible (this is what we measure)
        var deleteRecords = new List<RequestRecord>(messageIds.Count);
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
                var task = ctx.AddTask($"[bold]Delete burst → {messageIds.Count} msgs[/]", maxValue: messageIds.Count);

                for (var i = 0; i < messageIds.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var record = await context.DeleteProbeMessageAsync(bot, botName, chatId, messageIds[i], i, ct);
                    deleteRecords.Add(record);
                    task.Increment(1);

                    if (record.IsRateLimited)
                        console.MarkupLine(
                            $"  [yellow]429 at delete #{i} — retry_after={record.RetryAfterSeconds}s[/]");
                }
            });

        var finishedAt = DateTimeOffset.UtcNow;
        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, deleteRecords);
    }
}
