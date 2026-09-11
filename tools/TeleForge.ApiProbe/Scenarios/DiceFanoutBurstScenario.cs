using Spectre.Console;
using TeleForge.ApiProbe.Core;

namespace TeleForge.ApiProbe.Scenarios;

public sealed class DiceFanoutBurstScenario : IProbeScenario
{
    public string Name => "Dice Fanout Burst";
    public string Description => "Burst-send 🎰 dice round-robin across multiple users. Tests global dice throughput.";

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

        console.MarkupLine($"[bold]🎰 Dice fanout burst: {count} dice across {chatIds.Count} user(s)[/]");

        await console.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[bold]🎰 Dice fanout → {chatIds.Count} chats[/]", maxValue: count);

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var chatId = chatIds[i % chatIds.Count];
                    var record = await context.SendDiceAsync(bot, botName, chatId, i, ct, "🎰");
                    records.Add(record);
                    task.Increment(1);

                    if (record.IsRateLimited)
                        console.MarkupLine(
                            $"  [yellow]429 at dice #{i} (chat {chatId}) — retry_after={record.RetryAfterSeconds}s[/]");
                }
            });

        var finishedAt = DateTimeOffset.UtcNow;

        var perChat = records.GroupBy(r => r.TargetChatId);
        foreach (var group in perChat)
        {
            var rl = group.Count(r => r.IsRateLimited);
            console.MarkupLine($"  Chat {group.Key}: {group.Count()} dice sent, {rl} rate-limited");
        }

        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, records);
    }
}
