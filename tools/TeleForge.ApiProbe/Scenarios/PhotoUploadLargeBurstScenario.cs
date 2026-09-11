using Spectre.Console;
using TeleForge.ApiProbe.Core;

namespace TeleForge.ApiProbe.Scenarios;

public sealed class PhotoUploadLargeBurstScenario : IProbeScenario
{
    public string Name => "Photo Upload Large Burst";

    public string Description =>
        "Burst-send N ~5MB photos via multipart upload to private chat. Tests if large file size affects rate limit threshold.";

    private const int PhotoSizeBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatId = context.Config.PrivateChatId;
        if (chatId == 0)
            throw new InvalidOperationException("PrivateChatId must be set in config.");

        var count = context.Config.Defaults.MessageCount;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;
        var records = new List<RequestRecord>(count);

        console.MarkupLine($"[dim]Generating {PhotoSizeBytes / (1024 * 1024)}MB test JPEG...[/]");
        var photoBytes = ProbeTestData.GenerateJpegBytes(PhotoSizeBytes);

        var startedAt = DateTimeOffset.UtcNow;

        await console.Progress()
            .AutoClear(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[bold]Large photo burst → chat {chatId}[/]", maxValue: count);

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var record = await context.SendPhotoByStreamAsync(bot, botName, chatId, photoBytes, null,
                        PhotoSizeBytes, i, ct);
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
