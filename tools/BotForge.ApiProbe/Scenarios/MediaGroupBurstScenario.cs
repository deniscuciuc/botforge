using BotForge.ApiProbe.Core;
using Spectre.Console;
using Telegram.Bot.Types;

namespace BotForge.ApiProbe.Scenarios;

public sealed class MediaGroupBurstScenario : IProbeScenario
{
    public string Name => "Media Group Burst";

    public string Description =>
        "Burst-send N media groups (albums of 3 photos each) to private chat. Tests if album = 1 or N rate-limit requests.";

    private const int PhotosPerAlbum = 3;
    private const int PhotoSizeBytes = 30 * 1024; // 30 KB per photo

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatId = context.Config.PrivateChatId;
        if (chatId == 0)
            throw new InvalidOperationException("PrivateChatId must be set in config.");

        var count = context.Config.Defaults.MessageCount;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;
        var records = new List<RequestRecord>(count);
        var albumPayloadBytes = (long)PhotosPerAlbum * PhotoSizeBytes;

        console.MarkupLine($"[dim]Generating {PhotosPerAlbum} × {PhotoSizeBytes / 1024}KB test JPEGs for albums...[/]");
        var photoBytes = new byte[PhotosPerAlbum][];
        for (var p = 0; p < PhotosPerAlbum; p++)
            photoBytes[p] = ProbeTestData.GenerateJpegBytes(PhotoSizeBytes);

        var startedAt = DateTimeOffset.UtcNow;

        await console.Progress()
            .AutoClear(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[bold]Album burst ({PhotosPerAlbum} photos each) → chat {chatId}[/]",
                    maxValue: count);

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    // Create fresh streams for each send
                    var streams = new List<MemoryStream>();
                    var album = new List<IAlbumInputMedia>();
                    for (var p = 0; p < PhotosPerAlbum; p++)
                    {
                        var stream = ProbeTestData.CreateJpegStream(photoBytes[p]);
                        streams.Add(stream);
                        album.Add(new InputMediaPhoto(InputFile.FromStream(stream, $"album_{i}_{p}.jpg")));
                    }

                    var record =
                        await context.SendMediaGroupAsync(bot, botName, chatId, album, albumPayloadBytes, i, ct);
                    records.Add(record);
                    task.Increment(1);

                    foreach (var s in streams) s.Dispose();

                    if (record.IsRateLimited)
                        console.MarkupLine($"  [yellow]429 at #{i} — retry_after={record.RetryAfterSeconds}s[/]");
                }
            });

        var finishedAt = DateTimeOffset.UtcNow;
        return ScenarioResult.FromRequests(Name, Description, startedAt, finishedAt, records);
    }
}
