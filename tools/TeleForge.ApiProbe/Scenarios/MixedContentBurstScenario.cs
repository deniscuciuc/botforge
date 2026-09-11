using Spectre.Console;
using TeleForge.ApiProbe.Core;

namespace TeleForge.ApiProbe.Scenarios;

public sealed class MixedContentBurstScenario : IProbeScenario
{
    public string Name => "Mixed Content Burst";

    public string Description =>
        "Burst-send alternating text/photo/document to private chat. Tests if mixing content types affects throttling.";

    private const int PhotoSizeBytes = 50 * 1024;
    private const int DocSizeBytes = 100 * 1024;

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatId = context.Config.PrivateChatId;
        if (chatId == 0)
            throw new InvalidOperationException("PrivateChatId must be set in config.");

        var count = context.Config.Defaults.MessageCount;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;
        var records = new List<RequestRecord>(count);

        console.MarkupLine("[dim]Generating test assets (50KB photo, 100KB doc)...[/]");
        var photoBytes = ProbeTestData.GenerateJpegBytes(PhotoSizeBytes);
        var docBytes = ProbeTestData.GenerateDocumentBytes(DocSizeBytes);

        var startedAt = DateTimeOffset.UtcNow;

        await console.Progress()
            .AutoClear(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[bold]Mixed burst (text/photo/doc) → chat {chatId}[/]", maxValue: count);

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    RequestRecord record;

                    switch (i % 3)
                    {
                        case 0: // text
                            var text = ProbeTestData.MediumText(i);
                            record = await context.SendCustomTextAsync(bot, botName, chatId, text, i, ct);
                            break;
                        case 1: // photo
                            record = await context.SendPhotoByStreamAsync(bot, botName, chatId, photoBytes, null,
                                PhotoSizeBytes, i, ct);
                            break;
                        default: // document
                            record = await context.SendDocumentByStreamAsync(bot, botName, chatId, docBytes,
                                $"mixed_{i}.bin", DocSizeBytes, i, ct);
                            break;
                    }

                    records.Add(record);
                    task.Increment(1);

                    if (record.IsRateLimited)
                        console.MarkupLine(
                            $"  [yellow]429 at #{i} ({record.ApiMethod}) — retry_after={record.RetryAfterSeconds}s[/]");
                }
            });

        var finishedAt = DateTimeOffset.UtcNow;
        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, records);
    }
}
