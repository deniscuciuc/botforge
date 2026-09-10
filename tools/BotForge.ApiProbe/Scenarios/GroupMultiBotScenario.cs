using BotForge.ApiProbe.Core;
using Spectre.Console;

namespace BotForge.ApiProbe.Scenarios;

public sealed class GroupMultiBotScenario : IProbeScenario
{
    public string Name => "Group Multi-Bot";

    public string Description =>
        "Multiple bots send to the same group concurrently. Tests if the 20/min limit is shared across bots.";

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatId = context.Config.GroupChatId;
        if (chatId == 0)
            throw new InvalidOperationException("GroupChatId must be set in config.");

        if (context.Bots.Count < 2)
        {
            console.MarkupLine(
                "[yellow]Only 1 bot configured. For meaningful multi-bot testing, configure 2+ bots.[/]");
            console.MarkupLine("[yellow]Running with single bot anyway...[/]");
        }

        var messagesPerBot = context.Config.Defaults.MessageCount;
        var allRecords = new List<RequestRecord>();
        var lockObj = new object();

        var startedAt = DateTimeOffset.UtcNow;

        console.MarkupLine(
            $"[bold]{context.Bots.Count} bot(s) sending {messagesPerBot} msgs each to group {chatId}[/]");

        // Each bot sends in its own task, concurrently with other bots
        var tasks = context.Bots.Select(async kvp =>
        {
            var (botName, bot) = kvp;
            var botRecords = new List<RequestRecord>();

            // ~10 msgs/min per bot (below individual limit, but combined may hit shared limit)
            var interval = TimeSpan.FromMilliseconds(6000);
            using var timer = new PeriodicTimer(interval);

            for (var i = 0; i < messagesPerBot; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (i > 0) await timer.WaitForNextTickAsync(ct);

                var record = await context.SendProbeMessageAsync(bot, botName, chatId, i, ct);
                botRecords.Add(record);

                if (record.IsRateLimited)
                    console.MarkupLine(
                        $"  [yellow]{botName}: 429 at #{i} — retry_after={record.RetryAfterSeconds}s[/]");
            }

            lock (lockObj)
            {
                allRecords.AddRange(botRecords);
            }

            var botRateLimited = botRecords.Count(r => r.IsRateLimited);
            console.MarkupLine($"  [bold]{botName}[/]: {botRecords.Count} sent, {botRateLimited} rate-limited");
        });

        await Task.WhenAll(tasks);

        var finishedAt = DateTimeOffset.UtcNow;

        // Sort by timestamp for proper ordering in results
        allRecords.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, allRecords);
    }
}
