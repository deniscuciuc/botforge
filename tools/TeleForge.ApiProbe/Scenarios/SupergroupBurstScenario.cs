using Spectre.Console;
using TeleForge.ApiProbe.Core;

namespace TeleForge.ApiProbe.Scenarios;

public sealed class SupergroupBurstScenario : IProbeScenario
{
    public string Name => "Supergroup Burst";

    public string Description =>
        "Fire N messages to a supergroup with no delay. Tests supergroup-specific rate limits.";

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatId = context.Config.SupergroupChatId;
        if (chatId == 0)
            throw new InvalidOperationException("SupergroupChatId must be set in config.");

        var count = context.Config.Defaults.MessageCount;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;
        var records = new List<RequestRecord>(count);

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
                var task = ctx.AddTask($"[bold]Supergroup burst → chat {chatId}[/]", maxValue: count);

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var record = await context.SendProbeMessageAsync(bot, botName, chatId, i, ct);
                    records.Add(record);
                    task.Increment(1);

                    if (record.IsRateLimited)
                        console.MarkupLine($"  [yellow]429 at #{i} — retry_after={record.RetryAfterSeconds}s[/]");
                }
            });

        var finishedAt = DateTimeOffset.UtcNow;
        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, records);
    }
}
