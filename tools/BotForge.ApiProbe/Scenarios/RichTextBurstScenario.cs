using BotForge.ApiProbe.Core;
using Spectre.Console;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotForge.ApiProbe.Scenarios;

public sealed class RichTextBurstScenario : IProbeScenario
{
    public string Name => "Rich Text Burst";

    public string Description =>
        "Burst-send N messages with HTML formatting + 3×3 inline keyboard. Tests if larger JSON payload affects limits.";

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatId = context.Config.PrivateChatId;
        if (chatId == 0)
            throw new InvalidOperationException("PrivateChatId must be set in config.");

        var count = context.Config.Defaults.MessageCount;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;
        var records = new List<RequestRecord>(count);

        // Build a 3×3 inline keyboard
        var keyboard = new InlineKeyboardMarkup(
            Enumerable.Range(0, 3).Select(row =>
                Enumerable.Range(0, 3).Select(col =>
                    InlineKeyboardButton.WithCallbackData($"Btn {row * 3 + col + 1}", $"probe_btn_{row}_{col}"))));

        var startedAt = DateTimeOffset.UtcNow;

        await console.Progress()
            .AutoClear(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[bold]Rich text burst → chat {chatId}[/]", maxValue: count);

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var html = $"""
                                <b>Probe #{i}</b>
                                <i>Rich text with formatting</i>

                                <code>Status: running | Index: {i} | Time: {DateTimeOffset.UtcNow:HH:mm:ss.fff}</code>

                                <blockquote>This is a blockquote to test extended formatting. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.</blockquote>

                                <pre>┌──────────┬──────────┐
                                │  Column1 │  Column2 │
                                ├──────────┼──────────┤
                                │  Value 1 │  Value 2 │
                                │  Value 3 │  Value 4 │
                                └──────────┴──────────┘</pre>

                                <a href="https://example.com">Link text</a> | <b><i>Bold italic</i></b> | <s>Strikethrough</s>
                                """;

                    var record = await context.SendCustomTextAsync(bot, botName, chatId, html, i, ct,
                        "HTML", keyboard);
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
