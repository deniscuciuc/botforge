using System.Diagnostics;
using System.Net;
using Spectre.Console;
using TeleForge.ApiProbe.Core;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace TeleForge.ApiProbe.Scenarios;

/// <summary>
///     Fires N rapid <c>sendMessageDraft</c> calls to a private chat, each with a unique
///     draft ID, to discover the hard rate limit for the method.
/// </summary>
public sealed class MessageDraftBurstScenario : IProbeScenario
{
    public string Name => "Message Draft Burst";

    public string Description =>
        "Fire N sendMessageDraft calls to a single private chat with no delay. " +
        "Each call uses a unique draft ID to simulate concurrent streaming sessions.";

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatId = context.Config.PrivateChatId;
        if (chatId == 0)
            throw new InvalidOperationException("PrivateChatId must be set in config.");

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
                var task = ctx.AddTask($"[bold]Draft Burst → chat {chatId}[/]", maxValue: count);

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var draftId = i + 1;
                    var text = $"[Draft #{i} @ {DateTimeOffset.UtcNow:HH:mm:ss.fff}]";
                    var timestamp = DateTimeOffset.UtcNow;
                    var sw = Stopwatch.StartNew();

                    try
                    {
                        await bot.SendMessageDraft(chatId, draftId, text, cancellationToken: ct);
                        sw.Stop();

                        records.Add(new RequestRecord
                        {
                            Index = i,
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
                            Index = i,
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
                        console.MarkupLine($"  [yellow]429 at #{i} — retry_after={apiEx.Parameters?.RetryAfter}s[/]");
                    }
                    catch (ApiRequestException apiEx)
                    {
                        sw.Stop();
                        records.Add(new RequestRecord
                        {
                            Index = i,
                            Timestamp = timestamp,
                            Latency = sw.Elapsed,
                            HttpStatus = (HttpStatusCode)apiEx.ErrorCode,
                            IsRateLimited = false,
                            ErrorMessage = apiEx.Message,
                            TargetChatId = chatId,
                            BotName = botName,
                            ApiMethod = ApiMethod.SendMessage
                        });
                    }

                    task.Increment(1);
                }
            });

        var finishedAt = DateTimeOffset.UtcNow;
        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, records);
    }
}
