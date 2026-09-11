using Spectre.Console;
using TeleForge.ApiProbe.Core;

namespace TeleForge.ApiProbe.Scenarios;

public sealed class TextSizeBurstScenario : IProbeScenario
{
    public string Name => "Text Size Burst";

    public string Description =>
        "Burst N messages at 3 text sizes (100, 2000, 4096 chars) to private chat. Compares 429 rates across sizes.";

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatId = context.Config.PrivateChatId;
        if (chatId == 0)
            throw new InvalidOperationException("PrivateChatId must be set in config.");

        var count = context.Config.Defaults.MessageCount;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;
        var records = new List<RequestRecord>(count * 3);

        var sizes = new[] { 100, 2000, 4096 };
        var startedAt = DateTimeOffset.UtcNow;

        await console.Progress()
            .AutoClear(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                foreach (var size in sizes)
                {
                    var task = ctx.AddTask($"[bold]Burst {size} chars → chat {chatId}[/]", maxValue: count);

                    for (var i = 0; i < count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var text = ProbeTestData.GenerateText(size, i);
                        var record = await context.SendCustomTextAsync(bot, botName, chatId, text,
                            i + (size == 2000 ? count : size == 4096 ? count * 2 : 0), ct);
                        records.Add(record);
                        task.Increment(1);

                        if (record.IsRateLimited)
                            console.MarkupLine(
                                $"  [yellow]429 at #{i} ({size} chars) — retry_after={record.RetryAfterSeconds}s[/]");
                    }

                    console.MarkupLine(
                        $"  [dim]{size} chars done: {records.Count(r => r.PayloadBytes >= size - 20 && r.IsRateLimited)} rate-limited[/]");
                }
            });

        var finishedAt = DateTimeOffset.UtcNow;
        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, records);
    }
}
