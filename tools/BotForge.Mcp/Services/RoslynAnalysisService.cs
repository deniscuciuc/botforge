using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BotForge.Mcp.Services;

public sealed class RoslynAnalysisService
{
    public AnalysisResult AnalyzeSource(string sourceCode)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();
        var result = new AnalysisResult();

        // Detect Telegram.Bot API calls
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var invocation in invocations)
        {
            var text = invocation.Expression.ToString();

            if (text.Contains("SendTextMessageAsync") || text.Contains("SendMessage"))
                result.ApiCalls.Add(new DetectedPattern("SendTextMessage",
                    invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "ITelegramMessageService.CreateMessage().WithText().SendAsync()"));

            else if (text.Contains("SendPhotoAsync") || text.Contains("SendPhoto"))
                result.ApiCalls.Add(new DetectedPattern("SendPhoto",
                    invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "ITelegramMessageService.CreateMessage().WithMedia(Photo).SendAsync()"));

            else if (text.Contains("SendVideoAsync") || text.Contains("SendVideo"))
                result.ApiCalls.Add(new DetectedPattern("SendVideo",
                    invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "ITelegramMessageService.CreateMessage().WithMedia(Video).SendAsync()"));

            else if (text.Contains("SendDocumentAsync") || text.Contains("SendDocument"))
                result.ApiCalls.Add(new DetectedPattern("SendDocument",
                    invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "ITelegramMessageService.CreateMessage().WithMedia(Document).SendAsync()"));

            else if (text.Contains("EditMessageTextAsync") || text.Contains("EditMessageText"))
                result.ApiCalls.Add(new DetectedPattern("EditMessageText",
                    invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "ITelegramMessageService.CreateMessage().EditMessage(messageId).WithText().SendAsync()"));

            else if (text.Contains("EditMessageReplyMarkupAsync") || text.Contains("EditMessageReplyMarkup"))
                result.ApiCalls.Add(new DetectedPattern("EditMessageReplyMarkup",
                    invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "ITelegramMessageService.CreateMessage().EditKeyboardOnly(messageId).SendAsync()"));

            else if (text.Contains("AnswerCallbackQueryAsync") || text.Contains("AnswerCallbackQuery"))
                result.ApiCalls.Add(new DetectedPattern("AnswerCallbackQuery",
                    invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "return CallbackResult.Alert(text) or CallbackResult.Ok()"));

            else if (text.Contains("AnswerInlineQueryAsync") || text.Contains("AnswerInlineQuery"))
                result.ApiCalls.Add(new DetectedPattern("AnswerInlineQuery",
                    invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "IInlineQueryHandler context — answer from handler"));

            else if (text.Contains("DeleteMessageAsync") || text.Contains("DeleteMessage"))
                result.ApiCalls.Add(new DetectedPattern("DeleteMessage",
                    invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "Direct Telegram.Bot call (no framework wrapper yet)"));

            else if (text.Contains("SendInvoiceAsync") || text.Contains("SendInvoice"))
                result.ApiCalls.Add(new DetectedPattern("SendInvoice",
                    invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "IInvoiceService.SendInvoiceAsync() with IInvoiceBuilder"));

            else if (text.Contains("CreateInvoiceLinkAsync"))
                result.ApiCalls.Add(new DetectedPattern("CreateInvoiceLink",
                    invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "IInvoiceService.CreateLinkAsync()"));
        }

        // Detect update handling patterns
        var memberAccess = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
        foreach (var access in memberAccess)
        {
            var expr = access.ToString();

            if (expr.Contains("update.Message") || expr.Contains("Update.Message"))
                result.UpdateHandling.Add(
                    "Message handling detected — map to ICommandHandler / ITextMessageHandler / IMediaHandler");

            if (expr.Contains("update.CallbackQuery") || expr.Contains("Update.CallbackQuery"))
                result.UpdateHandling.Add("CallbackQuery handling detected — map to ICallbackQueryHandler");

            if (expr.Contains("update.InlineQuery") || expr.Contains("Update.InlineQuery"))
                result.UpdateHandling.Add("InlineQuery handling detected — map to IInlineQueryHandler");

            if (expr.Contains("update.PreCheckoutQuery") || expr.Contains("Update.PreCheckoutQuery"))
                result.UpdateHandling.Add(
                    "PreCheckoutQuery handling detected — map to IPreCheckoutValidator via AddTelegramPayments()");

            if (expr.Contains("update.Message.SuccessfulPayment"))
                result.UpdateHandling.Add(
                    "SuccessfulPayment handling detected — map to IPaymentProcessor via AddTelegramPayments()");
        }

        // Detect keyboard construction
        var objectCreations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
        foreach (var creation in objectCreations)
        {
            var typeName = creation.Type.ToString();

            if (typeName.Contains("InlineKeyboardMarkup"))
                result.KeyboardPatterns.Add(new DetectedPattern("InlineKeyboardMarkup construction",
                    creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "Use YAML template with buttons/dynamicButtons sections, or .WithInlineKeyboard()"));

            if (typeName.Contains("InlineKeyboardButton"))
                result.KeyboardPatterns.Add(new DetectedPattern("InlineKeyboardButton construction",
                    creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "Use YAML template button definition with type/value/text"));

            if (typeName.Contains("ReplyKeyboardMarkup"))
                result.KeyboardPatterns.Add(new DetectedPattern("ReplyKeyboardMarkup construction",
                    creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "Use .WithReplyKeyboard(rows, resize, oneTime, placeholder)"));
        }

        // Detect static method calls for keyboard buttons
        foreach (var invocation in invocations)
        {
            var text = invocation.Expression.ToString();
            if (text.Contains("WithCallbackData"))
                result.KeyboardPatterns.Add(new DetectedPattern("InlineKeyboardButton.WithCallbackData",
                    invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "Template button: type: Callback, value: \"action:{{param}}\""));
            if (text.Contains("WithUrl"))
                result.KeyboardPatterns.Add(new DetectedPattern("InlineKeyboardButton.WithUrl",
                    invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "Template button: type: Url, value: \"https://...\""));
        }

        // Detect state management patterns (userId dictionaries, etc.)
        var fields = root.DescendantNodes().OfType<FieldDeclarationSyntax>();
        foreach (var field in fields)
        {
            var typeText = field.Declaration.Type.ToString();
            if (typeText.Contains("Dictionary") && (typeText.Contains("long") || typeText.Contains("Int64")))
                result.StateManagement.Add(new DetectedPattern(
                    "User-keyed dictionary state",
                    field.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "Replace with IConversationHandler + IConversationStateStore for step-based flows, or INavigationService for menu navigation"));
            if (typeText.Contains("ConcurrentDictionary"))
                result.StateManagement.Add(new DetectedPattern(
                    "Concurrent user state dictionary",
                    field.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "Replace with IConversationHandler + IConversationStateStore (thread-safe by design)"));
        }

        // Detect switch on update type or callback data
        var switchStatements = root.DescendantNodes().OfType<SwitchStatementSyntax>();
        foreach (var sw in switchStatements)
        {
            var expr = sw.Expression.ToString();
            if (expr.Contains("update") || expr.Contains("Update") || expr.Contains("Type"))
                result.RoutingPatterns.Add(
                    "Switch on update type — replace with separate handler classes per update type");
            if (expr.Contains("CallbackData") || expr.Contains("callbackData") || expr.Contains("Data"))
                result.RoutingPatterns.Add(
                    "Switch on callback data — replace with separate ICallbackQueryHandler per pattern + [CallbackQuery] attribute");
        }

        // Also check switch expressions
        var switchExpressions = root.DescendantNodes().OfType<SwitchExpressionSyntax>();
        foreach (var sw in switchExpressions)
        {
            var expr = sw.GoverningExpression.ToString();
            if (expr.Contains("update") || expr.Contains("callbackData") || expr.Contains("CallbackData"))
                result.RoutingPatterns.Add(
                    "Switch expression on update/callback data — replace with separate handler classes");
        }

        // Detect TelegramBotClient construction
        foreach (var creation in objectCreations)
            if (creation.Type.ToString().Contains("TelegramBotClient"))
                result.ConfigPatterns.Add(new DetectedPattern(
                    "TelegramBotClient direct construction",
                    creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "Replace with BotConfiguration in appsettings.json + ITelegramBotClientProvider"));

        // Detect polling setup
        foreach (var invocation in invocations)
        {
            var text = invocation.Expression.ToString();
            if (text.Contains("StartReceiving") || text.Contains("ReceiveAsync"))
                result.ConfigPatterns.Add(new DetectedPattern(
                    "Manual polling setup",
                    invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    "Replace with AddTelegramConsumer(options => { options.DefaultTransport = UpdateTransport.LongPolling; })"));
        }

        // Deduplicate
        result.UpdateHandling = result.UpdateHandling.Distinct().ToList();
        result.RoutingPatterns = result.RoutingPatterns.Distinct().ToList();

        return result;
    }

    public string FormatResult(AnalysisResult result)
    {
        var sb = new StringBuilder();

        if (result.ApiCalls.Count > 0)
        {
            sb.AppendLine("## Telegram API Calls Detected");
            foreach (var call in result.ApiCalls)
                sb.AppendLine($"  - Line {call.Line}: `{call.Name}` → {call.Suggestion}");
            sb.AppendLine();
        }

        if (result.UpdateHandling.Count > 0)
        {
            sb.AppendLine("## Update Handling Patterns");
            foreach (var pattern in result.UpdateHandling)
                sb.AppendLine($"  - {pattern}");
            sb.AppendLine();
        }

        if (result.KeyboardPatterns.Count > 0)
        {
            sb.AppendLine("## Keyboard Construction");
            foreach (var kp in result.KeyboardPatterns)
                sb.AppendLine($"  - Line {kp.Line}: `{kp.Name}` → {kp.Suggestion}");
            sb.AppendLine();
        }

        if (result.StateManagement.Count > 0)
        {
            sb.AppendLine("## State Management");
            foreach (var sm in result.StateManagement)
                sb.AppendLine($"  - Line {sm.Line}: `{sm.Name}` → {sm.Suggestion}");
            sb.AppendLine();
        }

        if (result.RoutingPatterns.Count > 0)
        {
            sb.AppendLine("## Routing/Dispatch Patterns");
            foreach (var rp in result.RoutingPatterns)
                sb.AppendLine($"  - {rp}");
            sb.AppendLine();
        }

        if (result.ConfigPatterns.Count > 0)
        {
            sb.AppendLine("## Configuration Patterns");
            foreach (var cp in result.ConfigPatterns)
                sb.AppendLine($"  - Line {cp.Line}: `{cp.Name}` → {cp.Suggestion}");
            sb.AppendLine();
        }

        if (sb.Length == 0)
            sb.AppendLine("No recognizable Telegram.Bot patterns detected in this source.");

        return sb.ToString();
    }
}

public sealed class AnalysisResult
{
    public List<DetectedPattern> ApiCalls { get; set; } = [];
    public List<string> UpdateHandling { get; set; } = [];
    public List<DetectedPattern> KeyboardPatterns { get; set; } = [];
    public List<DetectedPattern> StateManagement { get; set; } = [];
    public List<string> RoutingPatterns { get; set; } = [];
    public List<DetectedPattern> ConfigPatterns { get; set; } = [];

    public string ComplexityEstimate
    {
        get
        {
            var total = ApiCalls.Count + KeyboardPatterns.Count + StateManagement.Count + RoutingPatterns.Count;
            return total switch
            {
                <= 3 => "Simple",
                <= 10 => "Moderate",
                _ => "Complex"
            };
        }
    }
}

public sealed record DetectedPattern(string Name, int Line, string Suggestion);
