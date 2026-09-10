using System.Diagnostics;
using System.Net;
using System.Text;
using BotForge.ApiProbe.Core;
using Spectre.Console;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace BotForge.ApiProbe.Scenarios;

/// <summary>
///     Simulates a realistic streaming session: one draft updated every 500 ms for ~26 s
///     (one draft ID), including a TTL-keepalive flush at 25 s.
///     Validates that the throttle cadence is stable and the keepalive logic prevents
///     draft expiry during long responses.
/// </summary>
public sealed class MessageDraftSustainedScenario : IProbeScenario
{
    public string Name => "Message Draft Sustained";

    public string Description =>
        "Simulate a realistic streaming session: one draft updated every 500 ms for ~26 s, " +
        "with a TTL-keepalive at 25 s. Validates throttle cadence and keepalive correctness.";

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatId = context.Config.PrivateChatId;
        if (chatId == 0)
            throw new InvalidOperationException("PrivateChatId must be set in config.");

        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;
        var records = new List<RequestRecord>();
        var draftId = Random.Shared.Next(1, int.MaxValue);

        var totalDuration = TimeSpan.FromSeconds(26); // slightly over the 25 s draft TTL
        var updateInterval = TimeSpan.FromMilliseconds(500);
        var expectedUpdates = (int)(totalDuration / updateInterval);

        var startedAt = DateTimeOffset.UtcNow;

        // Initial "thinking" draft (empty text → Telegram shows placeholder).
        await SendDraftUpdate(bot, botName, chatId, draftId, string.Empty, 0, records, ct);
        console.MarkupLine($"[grey]Initial draft sent. Streaming for {totalDuration.TotalSeconds} s at 500 ms intervals...[/]");

        await console.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[bold]Draft updates[/]", maxValue: expectedUpdates);
                var sb = new StringBuilder();
                var index = 1;
                var elapsed = TimeSpan.Zero;

                while (elapsed < totalDuration)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(updateInterval, ct);
                    elapsed += updateInterval;

                    sb.Append($"token_{index} ");

                    var isKeepalive = elapsed >= TimeSpan.FromSeconds(25);
                    await SendDraftUpdate(bot, botName, chatId, draftId, sb.ToString(), index, records, ct);

                    var label = isKeepalive ? " [keepalive]" : string.Empty;
                    console.MarkupLine(
                        $"  [grey]Update #{index}{label} (+{elapsed.TotalSeconds:F1} s) — {sb.Length} chars[/]");

                    task.Increment(1);
                    index++;
                }
            });

        // Send a visible cleanup message to replace the draft once the probe is done.
        try
        {
            await bot.SendMessage(chatId, "[Probe complete — draft stream ended]", cancellationToken: ct);
        }
        catch
        {
            // Non-fatal: only for human readability in the test chat.
        }

        var finishedAt = DateTimeOffset.UtcNow;
        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, records);
    }

    private static async Task SendDraftUpdate(
        TelegramBotClient bot,
        string botName,
        long chatId,
        int draftId,
        string text,
        int index,
        List<RequestRecord> records,
        CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var sw = Stopwatch.StartNew();

        try
        {
            await bot.SendMessageDraft(chatId, draftId, text, cancellationToken: ct);
            sw.Stop();

            records.Add(new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = HttpStatusCode.OK,
                IsRateLimited = false,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendMessage
            });
        }
        catch (ApiRequestException apiEx) when (apiEx.ErrorCode == 429)
        {
            sw.Stop();
            records.Add(new RequestRecord
            {
                Index = index,
                Timestamp = timestamp,
                Latency = sw.Elapsed,
                HttpStatus = (HttpStatusCode)429,
                IsRateLimited = true,
                RetryAfterSeconds = apiEx.Parameters?.RetryAfter,
                ErrorMessage = apiEx.Message,
                TargetChatId = chatId,
                BotName = botName,
                ApiMethod = ApiMethod.SendMessage
            });
        }
    }
}
