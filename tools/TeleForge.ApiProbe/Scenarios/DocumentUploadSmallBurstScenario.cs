using Spectre.Console;
using TeleForge.ApiProbe.Core;

namespace TeleForge.ApiProbe.Scenarios;

public sealed class DocumentUploadSmallBurstScenario : IProbeScenario
{
    public string Name => "Document Upload Small Burst";

    public string Description =>
        "Burst-send N ~100KB documents via multipart upload to private chat. Tests document upload rate limits.";

    private const int DocSizeBytes = 100 * 1024; // 100 KB

    public async Task<ScenarioResult> RunAsync(ProbeContext context, IAnsiConsole console, CancellationToken ct)
    {
        var chatId = context.Config.PrivateChatId;
        if (chatId == 0)
            throw new InvalidOperationException("PrivateChatId must be set in config.");

        var count = context.Config.Defaults.MessageCount;
        var bot = context.PrimaryBot;
        var botName = context.PrimaryBotName;
        var records = new List<RequestRecord>(count);

        console.MarkupLine($"[dim]Generating {DocSizeBytes / 1024}KB test document...[/]");
        var docBytes = ProbeTestData.GenerateDocumentBytes(DocSizeBytes);

        var startedAt = DateTimeOffset.UtcNow;

        await console.Progress()
            .AutoClear(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[bold]Document upload burst → chat {chatId}[/]", maxValue: count);

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var record = await context.SendDocumentByStreamAsync(bot, botName, chatId, docBytes,
                        $"probe_{i}.bin", DocSizeBytes, i, ct);
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
