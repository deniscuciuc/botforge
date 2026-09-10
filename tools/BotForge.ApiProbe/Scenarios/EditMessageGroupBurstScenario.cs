using BotForge.ApiProbe.Core;
using Spectre.Console;

namespace BotForge.ApiProbe.Scenarios;

public sealed class EditMessageGroupBurstScenario : IProbeScenario
{
    public string Name => "Edit Message Burst (Group)";
    public string Description => "Send 1 message to group then edit it N times. Tests edit rate limits in groups.";

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatId = context.Config.GroupChatId;
        if (chatId == 0)
            throw new InvalidOperationException("GroupChatId must be set in config.");

        var count = context.Config.Defaults.MessageCount;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;
        var records = new List<RequestRecord>(count);

        console.MarkupLine("[bold]Sending seed message to group...[/]");
        var seed = await context.SendProbeMessageAsync(bot, botName, chatId, 0, ct);
        if (seed.MessageId is null)
            throw new InvalidOperationException("Failed to send seed message to group.");

        var messageId = seed.MessageId.Value;
        console.MarkupLine($"[green]Seed message #{messageId} sent. Starting burst edits...[/]");

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
                var task = ctx.AddTask($"[bold]Edit burst (group) → msg {messageId}[/]", maxValue: count);

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var record = await context.EditProbeMessageAsync(bot, botName, chatId, messageId, i, ct);
                    records.Add(record);
                    task.Increment(1);

                    if (record.IsRateLimited)
                        console.MarkupLine($"  [yellow]429 at edit #{i} — retry_after={record.RetryAfterSeconds}s[/]");
                }
            });

        var finishedAt = DateTimeOffset.UtcNow;
        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, records);
    }
}
