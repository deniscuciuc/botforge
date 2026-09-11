using Spectre.Console;
using TeleForge.ApiProbe.Core;

namespace TeleForge.ApiProbe.Scenarios;

public sealed class GlobalLimitBurstScenario : IProbeScenario
{
    public string Name => "Global Limit Burst";

    public string Description =>
        "Send to ALL configured chats simultaneously via Task.WhenAll. Saturates the absolute per-bot throughput and shows per-chat-type breakdown.";

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var config = context.Config;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;

        // Collect all configured chat IDs with labels
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
            throw new InvalidOperationException(
                "No chat IDs configured. Set at least one of PrivateChatId, GroupChatId, SupergroupChatId, ChannelChatId, or FanoutChatIds.");

        var count = config.Defaults.MessageCount;
        console.MarkupLine($"[bold]Global burst: {count} messages across {chatTargets.Count} chats (simultaneous)[/]");
        foreach (var (chatId, label) in chatTargets)
            console.MarkupLine($"  {label}: {chatId}");

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
                var task = ctx.AddTask($"[bold]Global burst → {chatTargets.Count} chats[/]", maxValue: count);

                // Distribute messages across all chats using concurrent sends
                var semaphore = new SemaphoreSlim(chatTargets.Count);
                var pending = new List<Task<RequestRecord>>();

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var chatTarget = chatTargets[i % chatTargets.Count];

                    await semaphore.WaitAsync(ct);
                    var index = i;
                    pending.Add(Task.Run(async () =>
                    {
                        try
                        {
                            return await context.SendProbeMessageAsync(bot, botName, chatTarget.ChatId, index, ct);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, ct));

                    // Harvest completed tasks periodically to track progress
                    if (pending.Count >= chatTargets.Count * 2)
                    {
                        var completed = await Task.WhenAll(pending);
                        foreach (var r in completed)
                        {
                            records.Add(r);
                            task.Increment(1);
                            if (r.IsRateLimited)
                                console.MarkupLine(
                                    $"  [yellow]429 at #{r.Index} (chat {r.TargetChatId}) — retry_after={r.RetryAfterSeconds}s[/]");
                        }

                        pending.Clear();
                    }
                }

                // Drain remaining
                if (pending.Count > 0)
                {
                    var remaining = await Task.WhenAll(pending);
                    foreach (var r in remaining)
                    {
                        records.Add(r);
                        task.Increment(1);
                        if (r.IsRateLimited)
                            console.MarkupLine(
                                $"  [yellow]429 at #{r.Index} (chat {r.TargetChatId}) — retry_after={r.RetryAfterSeconds}s[/]");
                    }
                }
            });

        var finishedAt = DateTimeOffset.UtcNow;

        // Per-chat breakdown with labels
        console.MarkupLine("[bold]Per-chat-type breakdown:[/]");
        var perChat = records.GroupBy(r => r.TargetChatId);
        foreach (var group in perChat)
        {
            var label = chatTargets.FirstOrDefault(t => t.ChatId == group.Key).Label ?? "unknown";
            var rl = group.Count(r => r.IsRateLimited);
            var ok = group.Count(r => !r.IsRateLimited && r.ErrorMessage is null);
            console.MarkupLine(
                $"  [{(rl > 0 ? "yellow" : "green")}]{label}[/] ({group.Key}): {group.Count()} sent, {ok} ok, {rl} rate-limited");
        }

        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, records);
    }
}
