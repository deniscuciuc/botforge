using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using TeleForge.Mcp.Services;

namespace TeleForge.Mcp.Tools;

[McpServerToolType]
public class MigrationTools(RoslynAnalysisService roslyn)
{
    [McpServerTool]
    [Description(
        "Audits a single C# source file for Telegram.Bot API patterns and suggests TeleForge equivalents. Provide either a file path or the source code directly.")]
    public string AuditCsharpFile(
        [Description("Absolute path to a .cs file to analyze")]
        string? filePath = null,
        [Description("C# source code to analyze (alternative to filePath)")]
        string? sourceCode = null)
    {
        if (sourceCode is null && filePath is not null)
        {
            if (!File.Exists(filePath))
                return $"File not found: {filePath}";
            sourceCode = File.ReadAllText(filePath);
        }

        if (string.IsNullOrWhiteSpace(sourceCode))
            return "Provide either 'filePath' or 'sourceCode' to analyze.";

        var result = roslyn.AnalyzeSource(sourceCode);
        var sb = new StringBuilder();

        if (filePath is not null)
            sb.AppendLine($"# Audit: {Path.GetFileName(filePath)}");
        else
            sb.AppendLine("# Audit Results");

        sb.AppendLine($"**Estimated complexity:** {result.ComplexityEstimate}");
        sb.AppendLine();
        sb.Append(roslyn.FormatResult(result));

        return sb.ToString();
    }

    [McpServerTool]
    [Description(
        "Scans an entire directory of C# files for Telegram.Bot patterns and produces a project-wide migration assessment with complexity estimate.")]
    public string AuditProjectDirectory(
        [Description("Absolute path to the project directory to scan")]
        string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return $"Directory not found: {directoryPath}";

        var files = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);
        if (files.Length == 0)
            return "No .cs files found in directory.";

        var sb = new StringBuilder();
        sb.AppendLine($"# Project Audit: {Path.GetFileName(directoryPath)}");
        sb.AppendLine($"**Files scanned:** {files.Length}");
        sb.AppendLine();

        var totalApiCalls = 0;
        var totalKeyboards = 0;
        var totalState = 0;
        var totalRouting = 0;
        var totalConfig = 0;
        var fileResults = new List<(string File, AnalysisResult Result)>();

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            var result = roslyn.AnalyzeSource(source);

            var hasPatterns = result.ApiCalls.Count + result.KeyboardPatterns.Count +
                result.StateManagement.Count + result.RoutingPatterns.Count +
                result.ConfigPatterns.Count + result.UpdateHandling.Count > 0;

            if (hasPatterns)
            {
                fileResults.Add((Path.GetRelativePath(directoryPath, file), result));
                totalApiCalls += result.ApiCalls.Count;
                totalKeyboards += result.KeyboardPatterns.Count;
                totalState += result.StateManagement.Count;
                totalRouting += result.RoutingPatterns.Count;
                totalConfig += result.ConfigPatterns.Count;
            }
        }

        // Summary
        var total = totalApiCalls + totalKeyboards + totalState + totalRouting + totalConfig;
        var complexity = total switch
        {
            0 => "None — no Telegram.Bot patterns detected",
            <= 5 => "Simple — straightforward migration",
            <= 15 => "Moderate — multiple handler types and messaging patterns",
            _ => "Complex — extensive API usage, state management, and routing"
        };

        sb.AppendLine($"**Overall complexity:** {complexity}");
        sb.AppendLine($"**Files with patterns:** {fileResults.Count} / {files.Length}");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine($"  - API calls to migrate: {totalApiCalls}");
        sb.AppendLine($"  - Keyboard constructions: {totalKeyboards}");
        sb.AppendLine($"  - State management patterns: {totalState}");
        sb.AppendLine($"  - Routing/dispatch patterns: {totalRouting}");
        sb.AppendLine($"  - Configuration patterns: {totalConfig}");
        sb.AppendLine();

        // Per-file breakdown
        if (fileResults.Count > 0)
        {
            sb.AppendLine("## Per-File Breakdown");
            foreach (var (file, result) in fileResults)
            {
                sb.AppendLine($"### {file} ({result.ComplexityEstimate})");
                sb.Append(roslyn.FormatResult(result));
            }
        }

        // Migration recommendations
        sb.AppendLine("## Migration Steps");
        sb.AppendLine("1. Create new TeleForge project with `scaffold_bot_project` tool");
        sb.AppendLine("2. For each handler pattern detected, create handler with `scaffold_handler` tool");
        sb.AppendLine("3. Replace InlineKeyboardMarkup construction with YAML templates (`scaffold_template`)");
        sb.AppendLine("4. Replace state dictionaries with IConversationHandler steps");
        sb.AppendLine("5. Replace direct botClient calls with ITelegramMessageService fluent builder");
        sb.AppendLine("6. Move bot token to appsettings.json BotConfiguration");
        sb.AppendLine("7. Use `migrate_file` prompt to convert individual files with AI assistance");

        return sb.ToString();
    }

    [McpServerTool]
    [Description(
        "Analyzes a specific method or class from a custom bot and suggests the best TeleForge handler type, including a transformed code example.")]
    public string SuggestHandlerMapping(
        [Description("C# source code of the method or class to map to a TeleForge handler")]
        string sourceCode)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
            return "Provide source code to analyze.";

        var result = roslyn.AnalyzeSource(sourceCode);
        var sb = new StringBuilder();
        sb.AppendLine("# Handler Mapping Suggestion");
        sb.AppendLine();

        // Determine best handler type
        var hasCommand = result.UpdateHandling.Any(u => u.Contains("Message")) &&
                         (sourceCode.Contains("StartsWith(\"/\"") || sourceCode.Contains("command") ||
                          sourceCode.Contains("Command"));
        var hasCallback = result.UpdateHandling.Any(u => u.Contains("CallbackQuery"));
        var hasInline = result.UpdateHandling.Any(u => u.Contains("InlineQuery"));
        var hasPayment = result.UpdateHandling.Any(u => u.Contains("PreCheckout") || u.Contains("SuccessfulPayment"));
        var hasState = result.StateManagement.Count > 0;

        if (hasPayment)
        {
            sb.AppendLine("**Recommended:** Payment handlers (IPreCheckoutValidator + IPaymentProcessor)");
            sb.AppendLine("Use `scaffold_payment_handler` tool or `add_payment_flow` prompt.");
        }
        else if (hasState)
        {
            sb.AppendLine("**Recommended:** `IConversationHandler` — multi-step flow with state management");
            sb.AppendLine("Use `scaffold_handler` with handlerType='conversation'.");
        }
        else if (hasCommand)
        {
            sb.AppendLine("**Recommended:** `ICommandHandler` with `[TelegramCommand]` attribute");
            sb.AppendLine("Use `scaffold_handler` with handlerType='command'.");
        }
        else if (hasCallback)
        {
            sb.AppendLine("**Recommended:** `ICallbackQueryHandler` with `[CallbackQuery]` attribute");
            sb.AppendLine("Use `scaffold_handler` with handlerType='callback'.");
        }
        else if (hasInline)
        {
            sb.AppendLine("**Recommended:** `IInlineQueryHandler`");
            sb.AppendLine("Use `scaffold_handler` with handlerType='inline'.");
        }
        else if (result.ApiCalls.Any(a =>
                     a.Name.Contains("SendPhoto") || a.Name.Contains("SendVideo") || a.Name.Contains("SendDocument")))
        {
            sb.AppendLine("**Recommended:** `IMediaHandler` for media processing");
            sb.AppendLine("Use `scaffold_handler` with handlerType='media'.");
        }
        else if (result.ApiCalls.Count > 0)
        {
            sb.AppendLine("**Recommended:** `ITextMessageHandler` or `ICommandHandler`");
            sb.AppendLine("Use `scaffold_handler` to generate the appropriate type.");
        }
        else
        {
            sb.AppendLine(
                "**No clear handler pattern detected.** The code may be utility/helper code that doesn't need a handler mapping.");
        }

        sb.AppendLine();
        sb.AppendLine("## Detected Patterns");
        sb.Append(roslyn.FormatResult(result));

        return sb.ToString();
    }
}
