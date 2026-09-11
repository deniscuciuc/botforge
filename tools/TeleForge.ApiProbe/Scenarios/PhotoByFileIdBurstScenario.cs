using Spectre.Console;
using TeleForge.ApiProbe.Core;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace TeleForge.ApiProbe.Scenarios;

public sealed class PhotoByFileIdBurstScenario : IProbeScenario
{
    public string Name => "Photo FileId Burst";

    public string Description =>
        "Upload one photo, then burst re-send N times by file_id. Tests if file_id sends bypass upload overhead.";

    private const int PhotoSizeBytes = 50 * 1024; // 50 KB

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatId = context.Config.PrivateChatId;
        if (chatId == 0)
            throw new InvalidOperationException("PrivateChatId must be set in config.");

        var count = context.Config.Defaults.MessageCount;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;
        var records = new List<RequestRecord>(count);

        // First: upload one photo to get a file_id
        console.MarkupLine("[dim]Uploading seed photo to get file_id...[/]");
        var photoBytes = ProbeTestData.GenerateJpegBytes(PhotoSizeBytes);

        string fileId;
        try
        {
            using var seedStream = new MemoryStream(photoBytes, false);
            var seedMsg = await bot.SendPhoto(chatId, InputFile.FromStream(seedStream, "seed.jpg"),
                cancellationToken: ct);
            context.TrackMessageId(seedMsg.Id);
            fileId = seedMsg.Photo!.Last().FileId;
            console.MarkupLine($"[green]Seed photo uploaded. file_id={fileId[..20]}...[/]");
        }
        catch (ApiRequestException ex)
        {
            console.MarkupLine($"[red]Failed to upload seed photo: {ex.Message}[/]");
            throw new InvalidOperationException("Cannot proceed without seed photo file_id.", ex);
        }

        var startedAt = DateTimeOffset.UtcNow;

        await console.Progress()
            .AutoClear(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[bold]Photo by file_id burst → chat {chatId}[/]", maxValue: count);

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var record = await context.SendPhotoByFileIdAsync(bot, botName, chatId, fileId, i, ct);
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
