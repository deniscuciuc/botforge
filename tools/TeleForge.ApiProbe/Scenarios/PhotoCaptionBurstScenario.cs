using Spectre.Console;
using TeleForge.ApiProbe.Core;

namespace TeleForge.ApiProbe.Scenarios;

public sealed class PhotoCaptionBurstScenario : IProbeScenario
{
    public string Name => "Photo Caption Burst";

    public string Description =>
        "Burst-send N small photos with max 1024-char captions. Tests if photo+caption has different limits.";

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

        console.MarkupLine("[dim]Generating 50KB test JPEG + 1024-char captions...[/]");
        var photoBytes = ProbeTestData.GenerateJpegBytes(PhotoSizeBytes);

        var startedAt = DateTimeOffset.UtcNow;

        await console.Progress()
            .AutoClear(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[bold]Photo+Caption burst → chat {chatId}[/]", maxValue: count);

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var caption = ProbeTestData.MaxCaption(i);
                    var record = await context.SendPhotoByStreamAsync(bot, botName, chatId, photoBytes, caption,
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
