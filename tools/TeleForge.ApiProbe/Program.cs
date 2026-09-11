using Microsoft.Extensions.Configuration;
using Spectre.Console;
using TeleForge.ApiProbe.Configuration;
using TeleForge.ApiProbe.Core;
using TeleForge.ApiProbe.Reporting;
using TeleForge.ApiProbe.Scenarios;

var console = AnsiConsole.Console;
var runAll = args.Contains("--run-all", StringComparer.OrdinalIgnoreCase);

// Parse --scenarios filter (e.g. --scenarios "TextSize,Photo")
string[]? scenarioFilter = null;
var scenarioFlagIndex = Array.FindIndex(args, a => a.Equals("--scenarios", StringComparison.OrdinalIgnoreCase));
if (scenarioFlagIndex >= 0 && scenarioFlagIndex + 1 < args.Length)
    scenarioFilter = args[scenarioFlagIndex + 1]
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

console.Write(new FigletText("API Probe").Color(Color.Cyan1));
console.MarkupLine("[dim]Telegram Bot API Rate Limit Testing Tool[/]");
console.WriteLine();

// ── Configuration ──────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", false)
    .Build();

var probeConfig = new ProbeConfiguration();
config.GetSection("Probe").Bind(probeConfig);

// ── Validate & connect bots ────────────────────────────────────────────
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    console.MarkupLine("[red]Cancellation requested...[/]");
};

ProbeContext context;
try
{
    console.MarkupLine("[bold]Connecting bots...[/]");
    context = await ProbeContext.CreateAsync(probeConfig, console, cts.Token);
    console.WriteLine();
}
catch (Exception ex)
{
    console.MarkupLine($"[red]Failed to initialize: {ex.Message}[/]");
    console.MarkupLine("[dim]Check your bot tokens in appsettings.json[/]");
    return 1;
}

// ── Build scenario registry ────────────────────────────────────────────
var allScenarios = new List<IProbeScenario>
{
    // SendMessage — Private
    new PrivateChatBurstScenario(),
    new PrivateChatSustainedScenario(),

    // SendMessage — Group / Supergroup / Channel
    new GroupBurstScenario(),
    new GroupSustainedScenario(),
    new GroupMultiBotScenario(),
    new SupergroupBurstScenario(),
    new SupergroupSustainedScenario(),
    new ChannelBurstScenario(),
    new ChannelSustainedScenario(),

    // SendMessage — Fanout (multi-user)
    new FanoutBurstScenario(),
    new FanoutSustainedScenario(),

    // EditMessage (crash game)
    new EditMessageBurstScenario(),
    new EditMessageSustainedScenario(),
    new EditMessageGroupBurstScenario(),
    new EditMessageChannelBurstScenario(),

    // SendDice (🎰)
    new DiceBurstScenario(),
    new DiceSustainedScenario(),
    new DiceFanoutBurstScenario(),

    // DeleteMessage
    new DeleteMessageBurstScenario(),
    new DeleteMessageFanoutBurstScenario(),

    // Global bot limit
    new GlobalLimitBurstScenario(),
    new GlobalLimitSustainedScenario(),

    // Text Size (payload size impact)
    new TextSizeBurstScenario(),
    new LargeTextSustainedScenario(),

    // Photo uploads
    new PhotoUploadSmallBurstScenario(),
    new PhotoUploadLargeBurstScenario(),
    new PhotoUrlBurstScenario(),
    new PhotoCaptionBurstScenario(),
    new PhotoByFileIdBurstScenario(),

    // Document uploads
    new DocumentUploadSmallBurstScenario(),
    new DocumentUploadLargeBurstScenario(),
    new DocumentUploadSustainedScenario(),

    // Mixed & Albums
    new MediaGroupBurstScenario(),
    new MixedContentBurstScenario(),
    new RichTextBurstScenario()
};

// ── Report writers ─────────────────────────────────────────────────────
var allWriters = new List<IReportWriter>
{
    new ConsoleReportWriter(console),
    new CsvReportWriter(),
    new JsonReportWriter(),
    new MarkdownReportWriter()
};

// ── Main loop ──────────────────────────────────────────────────────────
while (!cts.Token.IsCancellationRequested)
{
    List<IProbeScenario> scenariosToRun;
    List<IReportWriter> writers;
    int messageCount;

    if (runAll)
    {
        // Headless mode: run all scenarios (or filtered subset) that have their chat IDs configured
        scenariosToRun = scenarioFilter is { Length: > 0 }
            ? allScenarios.Where(s => scenarioFilter.Any(f => s.Name.Contains(f, StringComparison.OrdinalIgnoreCase)))
                .ToList()
            : allScenarios;
        writers = allWriters.ToList();
        messageCount = probeConfig.Defaults.MessageCount;
        var filterLabel = scenarioFilter is { Length: > 0 } ? $"filter: {string.Join(", ", scenarioFilter)}" : "all";
        console.MarkupLine(
            $"[bold cyan]--run-all mode: {scenariosToRun.Count} scenarios ({filterLabel}), {messageCount} msgs each, all export formats[/]");
    }
    else
    {
        // Interactive mode
        var selectedScenarios = console.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[bold]Select scenarios to run:[/]")
                .PageSize(25)
                .AddChoiceGroup("SendMessage — Private",
                    allScenarios.Where(s => s.Name.StartsWith("Private Chat"))
                        .Select(s => $"{s.Name} — {s.Description}"))
                .AddChoiceGroup("SendMessage — Group/Supergroup/Channel",
                    allScenarios
                        .Where(s => s.Name.StartsWith("Group") || s.Name.StartsWith("Supergroup") ||
                                    s.Name.StartsWith("Channel")).Select(s => $"{s.Name} — {s.Description}"))
                .AddChoiceGroup("SendMessage — Fanout",
                    allScenarios.Where(s => s.Name.StartsWith("Fanout")).Select(s => $"{s.Name} — {s.Description}"))
                .AddChoiceGroup("EditMessage (crash game)",
                    allScenarios.Where(s => s.Name.StartsWith("Edit")).Select(s => $"{s.Name} — {s.Description}"))
                .AddChoiceGroup("SendDice (🎰)",
                    allScenarios.Where(s => s.Name.StartsWith("Dice")).Select(s => $"{s.Name} — {s.Description}"))
                .AddChoiceGroup("DeleteMessage",
                    allScenarios.Where(s => s.Name.StartsWith("Delete")).Select(s => $"{s.Name} — {s.Description}"))
                .AddChoiceGroup("Global Bot Limit",
                    allScenarios.Where(s => s.Name.StartsWith("Global")).Select(s => $"{s.Name} — {s.Description}"))
                .AddChoiceGroup("Text Size / Payload",
                    allScenarios
                        .Where(s => s.Name.Contains("Text Size") || s.Name.Contains("Large Text") ||
                                    s.Name.Contains("Rich Text")).Select(s => $"{s.Name} — {s.Description}"))
                .AddChoiceGroup("Photo Upload",
                    allScenarios.Where(s => s.Name.Contains("Photo")).Select(s => $"{s.Name} — {s.Description}"))
                .AddChoiceGroup("Document Upload",
                    allScenarios.Where(s => s.Name.Contains("Document")).Select(s => $"{s.Name} — {s.Description}"))
                .AddChoiceGroup("Mixed & Albums",
                    allScenarios.Where(s => s.Name.Contains("Media Group") || s.Name.Contains("Mixed"))
                        .Select(s => $"{s.Name} — {s.Description}"))
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]"));

        if (selectedScenarios.Count == 0)
        {
            console.MarkupLine("[yellow]No scenarios selected.[/]");
            continue;
        }

        scenariosToRun = allScenarios
            .Where(s => selectedScenarios.Any(sel => sel.StartsWith(s.Name, StringComparison.Ordinal)))
            .ToList();

        // Override message count
        messageCount = console.Prompt(
            new TextPrompt<int>("[bold]Message count per scenario?[/]")
                .DefaultValue(probeConfig.Defaults.MessageCount)
                .Validate(n => n is > 0 and <= 1000
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Must be 1-1000")));
        probeConfig.Defaults.MessageCount = messageCount;

        // Select report formats
        var selectedFormats = console.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[bold]Export formats:[/]")
                .AddChoices(allWriters.Select(w => w.Format))
                .Select("Console"));

        writers = allWriters.Where(w => selectedFormats.Contains(w.Format)).ToList();
    }

    // Show config summary
    console.WriteLine();
    var configTable = new Table().Border(TableBorder.Rounded)
        .AddColumn("Setting").AddColumn("Value");
    configTable.AddRow("Scenarios", string.Join(", ", scenariosToRun.Select(s => s.Name)));
    configTable.AddRow("Messages/Scenario", messageCount.ToString());
    configTable.AddRow("Bots", string.Join(", ", context.Bots.Keys));
    configTable.AddRow("Private Chat ID", probeConfig.PrivateChatId.ToString());
    configTable.AddRow("Group Chat ID", probeConfig.GroupChatId.ToString());
    configTable.AddRow("Supergroup Chat ID", probeConfig.SupergroupChatId != 0
        ? probeConfig.SupergroupChatId.ToString()
        : "[dim](not set)[/]");
    configTable.AddRow("Channel Chat ID", probeConfig.ChannelChatId != 0
        ? probeConfig.ChannelChatId.ToString()
        : "[dim](not set)[/]");
    configTable.AddRow("Fanout Chat IDs", probeConfig.FanoutChatIds.Count > 0
        ? string.Join(", ", probeConfig.FanoutChatIds)
        : "[dim](none)[/]");
    configTable.AddRow("Export Formats", string.Join(", ", writers.Select(w => w.Format)));
    configTable.AddRow("Output Directory", probeConfig.OutputDirectory);
    configTable.AddRow("Cooldown Between", $"{probeConfig.Defaults.CooldownSeconds}s");
    console.Write(configTable);

    if (!runAll && !console.Confirm("[bold]Start run?[/]"))
        continue;

    // ── Run scenarios ──────────────────────────────────────────────────
    var results = new List<ScenarioResult>();

    for (var i = 0; i < scenariosToRun.Count; i++)
    {
        var scenario = scenariosToRun[i];
        console.WriteLine();
        console.Write(new Rule($"[bold cyan]({i + 1}/{scenariosToRun.Count}) {scenario.Name}[/]").RuleStyle("cyan"));
        console.MarkupLine($"[dim]{scenario.Description}[/]");
        console.WriteLine();

        try
        {
            var result = await scenario.RunAsync(context, console, cts.Token);
            results.Add(result);

            // Write reports for this scenario
            foreach (var writer in writers)
                try
                {
                    await writer.WriteAsync(result, probeConfig.OutputDirectory);
                }
                catch (Exception ex)
                {
                    console.MarkupLine($"[red]{writer.Format} report failed: {ex.Message}[/]");
                }
        }
        catch (OperationCanceledException)
        {
            console.MarkupLine("[red]Scenario cancelled.[/]");
            break;
        }
        catch (InvalidOperationException ex)
        {
            console.MarkupLine($"[red]Scenario skipped: {ex.Message}[/]");
            continue;
        }
        catch (Exception ex)
        {
            console.MarkupLine($"[red]Scenario failed: {ex.Message}[/]");
            continue;
        }

        // Cooldown between scenarios
        if (i < scenariosToRun.Count - 1 && probeConfig.Defaults.CooldownSeconds > 0)
            await console.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Cooldown {probeConfig.Defaults.CooldownSeconds}s before next scenario...",
                    async _ => await Task.Delay(
                        TimeSpan.FromSeconds(probeConfig.Defaults.CooldownSeconds), cts.Token));
    }

    // ── Final summary ──────────────────────────────────────────────────
    if (results.Count > 1)
    {
        console.WriteLine();
        console.Write(new Rule("[bold green]Run Summary[/]").RuleStyle("green"));

        var summaryTable = new Table()
            .Border(TableBorder.HeavyHead)
            .AddColumn("Scenario")
            .AddColumn("Total")
            .AddColumn("OK")
            .AddColumn("429s")
            .AddColumn("Errors")
            .AddColumn("RPS")
            .AddColumn("P50")
            .AddColumn("P95")
            .AddColumn("Threshold");

        foreach (var r in results)
            summaryTable.AddRow(
                r.ScenarioName,
                r.TotalRequests.ToString(),
                $"[green]{r.Succeeded}[/]",
                $"[yellow]{r.RateLimited}[/]",
                $"[red]{r.OtherErrors}[/]",
                $"{r.ActualRps:F1}",
                $"{r.P50LatencyMs:F0}ms",
                $"{r.P95LatencyMs:F0}ms",
                r.ThresholdRps.HasValue ? $"{r.ThresholdRps.Value:F1}" : "-");

        console.Write(summaryTable);
    }

    // ── Cleanup ────────────────────────────────────────────────────────
    if (probeConfig.Defaults.CleanupAfterRun && context.SentMessageIds.Count > 0)
    {
        var doCleanup = runAll || console.Confirm($"[yellow]Delete {context.SentMessageIds.Count} test messages?[/]");
        if (doCleanup)
        {
            // Clean from private chat if used
            if (probeConfig.PrivateChatId != 0)
                await context.CleanupAsync(probeConfig.PrivateChatId, console, cts.Token);

            if (probeConfig.GroupChatId != 0)
                await context.CleanupAsync(probeConfig.GroupChatId, console, cts.Token);

            if (probeConfig.SupergroupChatId != 0)
                await context.CleanupAsync(probeConfig.SupergroupChatId, console, cts.Token);

            if (probeConfig.ChannelChatId != 0)
                await context.CleanupAsync(probeConfig.ChannelChatId, console, cts.Token);

            foreach (var fanoutId in probeConfig.FanoutChatIds)
                await context.CleanupAsync(fanoutId, console, cts.Token);
        }
    }

    console.WriteLine();
    if (writers.Any(w => w.Format != "Console"))
        console.MarkupLine($"[green]Reports saved to: {Path.GetFullPath(probeConfig.OutputDirectory)}[/]");

    // Run again?
    if (runAll || !console.Confirm("[bold]Run more scenarios?[/]", false))
        break;
}

console.MarkupLine("[dim]Goodbye![/]");
return 0;
