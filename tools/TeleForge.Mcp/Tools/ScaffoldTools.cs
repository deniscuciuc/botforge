using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace TeleForge.Mcp.Tools;

[McpServerToolType]
public static class ScaffoldTools
{
    [McpServerTool]
    [Description(
        "Generates a complete C# handler file for TeleForge. Returns ready-to-use source code with correct interface, attribute, context, result, and using statements.")]
    public static string ScaffoldHandler(
        [Description("Handler type: command, callback, text, media, inline, dice, conversation")]
        string handlerType,
        [Description("Class name for the handler (e.g., StartHandler, BuyCallbackHandler)")]
        string name,
        [Description(
            "Command name (for command type, e.g., 'start') or callback pattern (for callback type, e.g., 'buy:{productId}')")]
        string commandOrPattern = "",
        [Description("Namespace for the generated class")]
        string ns = "MyBot.Handlers",
        [Description("Authorization policy name (optional, e.g., 'admin')")]
        string? authorize = null,
        [Description("Rate limit in seconds (optional, 0 = no limit)")]
        int rateLimit = 0,
        [Description("Allowed chat types (optional, comma-separated, e.g., 'private,group')")]
        string? chatTypes = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using TeleForge.Routing.Handlers;");
        sb.AppendLine("using TeleForge.Routing.Attributes;");
        sb.AppendLine("using TeleForge.Messaging.Abstractions;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();

        var attributes = new List<string>();
        string iface, method, contextType, returnType, resultUsage;

        switch (handlerType.ToLowerInvariant())
        {
            case "command":
                var cmd = string.IsNullOrEmpty(commandOrPattern) ? "mycommand" : commandOrPattern;
                iface = "ICommandHandler";
                method = "HandleAsync";
                contextType = "CommandContext";
                returnType = "Task<CommandResult>";
                resultUsage = "return CommandResult.Ok();";
                attributes.Add($"[TelegramCommand(\"{cmd}\")]");
                break;

            case "callback":
                var pattern = string.IsNullOrEmpty(commandOrPattern) ? "action:{id}" : commandOrPattern;
                iface = "ICallbackQueryHandler";
                method = "HandleAsync";
                contextType = "CallbackContext";
                returnType = "Task<CallbackResult>";
                resultUsage = "return CallbackResult.Ok();";
                attributes.Add($"[CallbackQuery(\"{pattern}\")]");
                break;

            case "text":
                iface = "ITextMessageHandler";
                method = "HandleAsync";
                contextType = "TextMessageContext";
                returnType = "Task<TextMessageResult>";
                resultUsage = "return TextMessageResult.Ok();";
                if (!string.IsNullOrEmpty(commandOrPattern))
                    attributes.Add($"[TextMessage(\"{commandOrPattern}\")]");
                else
                    attributes.Add("[TextMessage]");
                break;

            case "media":
                iface = "IMediaHandler";
                method = "HandleAsync";
                contextType = "MediaContext";
                returnType = "Task";
                resultUsage = "";
                break;

            case "inline":
                iface = "IInlineQueryHandler";
                method = "HandleAsync";
                contextType = "InlineQueryContext";
                returnType = "Task";
                resultUsage = "";
                break;

            case "dice":
                iface = "IDiceHandler";
                method = "HandleAsync";
                contextType = "DiceContext";
                returnType = "Task";
                resultUsage = "";
                break;

            case "conversation":
                iface = "IConversationHandler";
                method = "HandleStepAsync";
                contextType = "ConversationStepContext";
                returnType = "Task<ConversationResult>";
                resultUsage = "return ConversationResult.Complete();";
                sb.Clear();
                sb.AppendLine("using TeleForge.Routing.Abstractions;");
                sb.AppendLine("using TeleForge.Messaging.Abstractions;");
                sb.AppendLine();
                sb.AppendLine($"namespace {ns};");
                sb.AppendLine();
                break;

            default:
                return
                    $"Unknown handler type: '{handlerType}'. Use: command, callback, text, media, inline, dice, conversation";
        }

        if (authorize is not null)
            attributes.Add($"[Authorize(\"{authorize}\")]");
        if (rateLimit > 0)
            attributes.Add($"[RateLimit({rateLimit})]");
        if (chatTypes is not null)
        {
            var types = string.Join("\", \"", chatTypes.Split(',', StringSplitOptions.TrimEntries));
            attributes.Add($"[ChatType(\"{types}\")]");
        }

        foreach (var attr in attributes)
            sb.AppendLine(attr);

        sb.AppendLine($"public class {name} : {iface}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly ITelegramMessageService _messageService;");
        sb.AppendLine();
        sb.AppendLine($"    public {name}(ITelegramMessageService messageService)");
        sb.AppendLine("    {");
        sb.AppendLine("        _messageService = messageService;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async {returnType} {method}({contextType} context, CancellationToken ct)");
        sb.AppendLine("    {");
        sb.AppendLine("        // TODO: Implement handler logic");

        if (handlerType.ToLowerInvariant() == "callback")
            sb.AppendLine("        // var id = context.GetRouteParam<int>(\"id\");");

        sb.AppendLine();
        sb.AppendLine("        await _messageService.CreateMessage(context.BotId)");
        sb.AppendLine("            .ToChat(context.ChatId!.Value)");
        sb.AppendLine("            .WithTemplate(\"response_template\")");
        sb.AppendLine("            .WithLanguage(context.UpdateContext.Language ?? \"en\")");
        sb.AppendLine("            .SendAsync(ct);");

        if (!string.IsNullOrEmpty(resultUsage))
        {
            sb.AppendLine();
            sb.AppendLine($"        {resultUsage}");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    [McpServerTool]
    [Description(
        "Generates a YAML template file for TeleForge with translations, optional static buttons, and optional dynamic buttons with pagination.")]
    public static string ScaffoldTemplate(
        [Description("Template name (e.g., 'welcome_message', 'shop_item')")]
        string name,
        [Description("Comma-separated language codes (e.g., 'en,ru')")]
        string languages = "en",
        [Description("Include static inline buttons")]
        bool hasButtons = false,
        [Description("Include dynamic data-driven buttons with pagination")]
        bool hasDynamicButtons = false,
        [Description("Parse mode: Html, Markdown, MarkdownV2, or None")]
        string parseMode = "Html")
    {
        var langs = languages.Split(',', StringSplitOptions.TrimEntries);
        var sb = new StringBuilder();

        sb.AppendLine($"name: {name}");
        sb.AppendLine($"parseMode: {parseMode}");
        sb.AppendLine("text:");
        foreach (var lang in langs)
        {
            sb.AppendLine($"  {lang}: |");
            sb.AppendLine($"    TODO: Add {lang} text here");
            sb.AppendLine($"    Use {{{{variable}}}} for parameters");
        }

        if (hasButtons)
        {
            sb.AppendLine("buttons:");
            sb.AppendLine("  - text:");
            foreach (var lang in langs)
                sb.AppendLine($"      {lang}: \"✅ Confirm\"");
            sb.AppendLine("    type: Callback");
            sb.AppendLine($"    value: \"{name}_confirm:{{{{id}}}}\"");
            sb.AppendLine("    row: 0");
            sb.AppendLine("    order: 0");
            sb.AppendLine("  - text:");
            foreach (var lang in langs)
                sb.AppendLine($"      {lang}: \"❌ Cancel\"");
            sb.AppendLine("    type: Callback");
            sb.AppendLine($"    value: \"{name}_cancel:{{{{id}}}}\"");
            sb.AppendLine("    row: 0");
            sb.AppendLine("    order: 1");
        }

        if (hasDynamicButtons)
        {
            sb.AppendLine("dynamicButtons:");
            sb.AppendLine("  - name: items");
            sb.AppendLine("    text:");
            foreach (var lang in langs)
                sb.AppendLine($"      {lang}: \"{{{{itemName}}}} — {{{{price}}}}⭐\"");
            sb.AppendLine("    type: Callback");
            sb.AppendLine($"    value: \"select_{name}:{{{{itemId}}}}\"");
            sb.AppendLine("    rowStart: 1");
            sb.AppendLine("    itemsPerRow: 1");
            sb.AppendLine("    itemsPerPage: 5");
            sb.AppendLine($"    paginationCallbackPrefix: \"{name}_page\"");
            sb.AppendLine("    paginationRow: 6");
            sb.AppendLine("    paginationButtons:");
            sb.AppendLine("      previous:");
            foreach (var lang in langs)
                sb.AppendLine($"        {lang}: \"⬅️\"");
            sb.AppendLine("      next:");
            foreach (var lang in langs)
                sb.AppendLine($"        {lang}: \"➡️\"");
            sb.AppendLine("      counter: \"{{current}}/{{total}}\"");
        }

        return sb.ToString();
    }

    [McpServerTool]
    [Description(
        "Generates a middleware class for the TeleForge update pipeline (ITelegramMiddleware) or send pipeline (ISendMiddleware).")]
    public static string ScaffoldMiddleware(
        [Description("Class name for the middleware")]
        string name,
        [Description("Pipeline type: 'update' for ITelegramMiddleware or 'send' for ISendMiddleware")]
        string pipelineType = "update",
        [Description("Namespace for the generated class")]
        string ns = "MyBot.Middleware")
    {
        if (pipelineType.Equals("send", StringComparison.OrdinalIgnoreCase))
            return $$"""
                     using TeleForge.Messaging.Abstractions;

                     namespace {{ns}};

                     public class {{name}} : ISendMiddleware
                     {
                         public async Task<SendResult> InvokeAsync(SendContext context, TelegramSendDelegate next)
                         {
                             // Pre-send logic (modify message, log, validate, etc.)

                             var result = await next(context);

                             // Post-send logic (metrics, error handling, etc.)

                             return result;
                         }
                     }
                     """;

        return $$"""
                 using TeleForge.Core;

                 namespace {{ns}};

                 public class {{name}} : ITelegramMiddleware
                 {
                     public async Task InvokeAsync(TelegramUpdateContext context, TelegramUpdateDelegate next)
                     {
                         // Pre-processing logic (auth, logging, locale resolution, etc.)

                         await next(context);

                         // Post-processing logic (cleanup, metrics, etc.)
                     }
                 }
                 """;
    }

    [McpServerTool]
    [Description(
        "Generates payment handler classes — validator, processor, and/or refund processor — for a given payload prefix, plus the DI registration snippet.")]
    public static string ScaffoldPaymentHandler(
        [Description("Payload prefix for routing (e.g., 'shop', 'donation', 'subscription')")]
        string payloadPrefix,
        [Description("Components to generate, comma-separated: validator, processor, refund")]
        string components = "validator,processor,refund",
        [Description("Namespace for generated classes")]
        string ns = "MyBot.Payments")
    {
        var parts = components.Split(',', StringSplitOptions.TrimEntries);
        var sb = new StringBuilder();

        sb.AppendLine("using TeleForge.Payments.Abstractions;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();

        // TypedPayload
        var payloadClass = char.ToUpper(payloadPrefix[0]) + payloadPrefix[1..] + "Payload";
        sb.AppendLine($"public class {payloadClass} : TypedPayload");
        sb.AppendLine("{");
        sb.AppendLine($"    public override string Prefix => \"{payloadPrefix}\";");
        sb.AppendLine("    public string ItemId { get; }");
        sb.AppendLine($"    public {payloadClass}(string itemId) => ItemId = itemId;");
        sb.AppendLine("    public override string[] SerializeFields() => [ItemId];");
        sb.AppendLine("}");
        sb.AppendLine();

        if (parts.Contains("validator", StringComparer.OrdinalIgnoreCase))
        {
            var className = char.ToUpper(payloadPrefix[0]) + payloadPrefix[1..] + "CheckoutValidator";
            sb.AppendLine($"public class {className} : IPreCheckoutValidator");
            sb.AppendLine("{");
            sb.AppendLine(
                "    public async Task<CheckoutValidationResult> ValidateAsync(PreCheckoutContext context, CancellationToken ct)");
            sb.AppendLine("    {");
            sb.AppendLine(
                $"        var payload = context.GetPayload<{payloadClass}>(fields => new {payloadClass}(fields[0]));");
            sb.AppendLine();
            sb.AppendLine("        // TODO: Validate stock, user eligibility, limits, etc.");
            sb.AppendLine();
            sb.AppendLine("        return CheckoutValidationResult.Approve();");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        if (parts.Contains("processor", StringComparer.OrdinalIgnoreCase))
        {
            var className = char.ToUpper(payloadPrefix[0]) + payloadPrefix[1..] + "PaymentProcessor";
            sb.AppendLine($"public class {className} : IPaymentProcessor");
            sb.AppendLine("{");
            sb.AppendLine("    public async Task ProcessAsync(SuccessfulPaymentContext context, CancellationToken ct)");
            sb.AppendLine("    {");
            sb.AppendLine(
                $"        var payload = context.GetPayload<{payloadClass}>(fields => new {payloadClass}(fields[0]));");
            sb.AppendLine();
            sb.AppendLine("        // TODO: Grant item/subscription, update database, send confirmation message");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        if (parts.Contains("refund", StringComparer.OrdinalIgnoreCase))
        {
            var className = char.ToUpper(payloadPrefix[0]) + payloadPrefix[1..] + "RefundProcessor";
            sb.AppendLine($"public class {className} : IRefundProcessor");
            sb.AppendLine("{");
            sb.AppendLine(
                "    public async Task<RefundDecision> ProcessAsync(RefundContext context, CancellationToken ct)");
            sb.AppendLine("    {");
            sb.AppendLine("        // TODO: Validate refund eligibility, reverse grants");
            sb.AppendLine();
            sb.AppendLine("        return RefundDecision.Approve(RefundReason.UserRequest);");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // Registration snippet
        sb.AppendLine("// --- DI Registration (add to Program.cs) ---");
        sb.AppendLine("// services.AddTelegramPayments(");
        sb.AppendLine("//     options => { options.AutoRefundOnFailure = true; },");
        sb.AppendLine("//     handlers =>");
        sb.AppendLine("//     {");
        if (parts.Contains("validator", StringComparer.OrdinalIgnoreCase))
            sb.AppendLine(
                $"//         handlers.AddValidator<{char.ToUpper(payloadPrefix[0]) + payloadPrefix[1..]}CheckoutValidator>(\"{payloadPrefix}\");");
        if (parts.Contains("processor", StringComparer.OrdinalIgnoreCase))
            sb.AppendLine(
                $"//         handlers.AddProcessor<{char.ToUpper(payloadPrefix[0]) + payloadPrefix[1..]}PaymentProcessor>(\"{payloadPrefix}\");");
        if (parts.Contains("refund", StringComparer.OrdinalIgnoreCase))
            sb.AppendLine(
                $"//         handlers.AddRefundProcessor<{char.ToUpper(payloadPrefix[0]) + payloadPrefix[1..]}RefundProcessor>(\"{payloadPrefix}\");");
        sb.AppendLine("//     });");

        return sb.ToString();
    }

    [McpServerTool]
    [Description(
        "Generates a complete bot project structure — Program.cs, appsettings.json, and csproj content — for a new TeleForge bot.")]
    public static string ScaffoldBotProject(
        [Description("Project name (e.g., 'MyTelegramBot')")]
        string name,
        [Description("Include payment system setup")]
        bool usePayments = false,
        [Description("Include template engine setup")]
        bool useTemplates = true,
        [Description("Consumer mode: polling, webhook, or queue")]
        string consumerMode = "polling")
    {
        var transport = consumerMode.ToLowerInvariant() switch
        {
            "webhook" => "UpdateTransport.Webhook",
            "queue" => "UpdateTransport.Queue",
            _ => "UpdateTransport.LongPolling"
        };

        var sb = new StringBuilder();
        sb.AppendLine($"// ============ {name}.csproj ============");
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <OutputType>Exe</OutputType>");
        sb.AppendLine("    <TargetFramework>net10.0</TargetFramework>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <PackageReference Include=\"TeleForge.Consumer\" />");
        sb.AppendLine("    <PackageReference Include=\"TeleForge.Routing\" />");
        sb.AppendLine("    <PackageReference Include=\"TeleForge.Messaging\" />");
        if (useTemplates)
            sb.AppendLine("    <PackageReference Include=\"TeleForge.Templates\" />");
        if (usePayments)
            sb.AppendLine("    <PackageReference Include=\"TeleForge.Payments\" />");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.Extensions.Hosting\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine("</Project>");
        sb.AppendLine();
        sb.AppendLine("// ============ Program.cs ============");
        sb.AppendLine("using Microsoft.Extensions.Hosting;");
        sb.AppendLine("using TeleForge.Consumer.Extensions;");
        sb.AppendLine("using TeleForge.Routing.Extensions;");
        sb.AppendLine("using TeleForge.Messaging;");
        if (useTemplates)
            sb.AppendLine("using TeleForge.Templates;");
        if (usePayments)
            sb.AppendLine("using TeleForge.Payments;");
        sb.AppendLine("using TeleForge.Core;");
        sb.AppendLine();
        sb.AppendLine("var builder = Host.CreateApplicationBuilder(args);");
        sb.AppendLine();
        sb.AppendLine("builder.Services.AddTelegramMessaging(options => { });");
        sb.AppendLine();
        sb.AppendLine("builder.Services.AddTelegramRouting(options =>");
        sb.AppendLine("{");
        sb.AppendLine("    options.HandlerAssemblies.Add(typeof(Program).Assembly);");
        sb.AppendLine("});");
        sb.AppendLine();
        if (useTemplates)
        {
            sb.AppendLine("builder.Services.AddTelegramTemplates(options =>");
            sb.AppendLine("{");
            sb.AppendLine("    options.DefaultLanguage = \"en\";");
            sb.AppendLine("    options.FallbackLanguage = \"en\";");
            sb.AppendLine("});");
            sb.AppendLine();
        }

        sb.AppendLine("builder.Services.AddTelegramConsumer(options =>");
        sb.AppendLine("{");
        sb.AppendLine($"    options.DefaultTransport = {transport};");
        if (consumerMode.Equals("webhook", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("    options.ConfigureWebhook(webhook =>");
            sb.AppendLine("    {");
            sb.AppendLine("        webhook.Path = \"/api/telegram/webhook/{botId}\";");
            sb.AppendLine("        webhook.SecretToken = builder.Configuration[\"Telegram:WebhookSecret\"];");
            sb.AppendLine("    });");
        }

        sb.AppendLine("});");
        sb.AppendLine();
        if (usePayments)
        {
            sb.AppendLine("builder.Services.AddTelegramPayments(");
            sb.AppendLine("    options => { options.AutoRefundOnFailure = true; },");
            sb.AppendLine("    handlers => { /* handlers.AddValidator<...>(\"prefix\"); */ });");
            sb.AppendLine();
        }

        sb.AppendLine("await builder.Build().RunAsync();");
        sb.AppendLine();
        sb.AppendLine("// ============ appsettings.json ============");
        sb.AppendLine("{");
        sb.AppendLine("  \"Bots\": [");
        sb.AppendLine("    {");
        sb.AppendLine($"      \"Key\": \"main\",");
        sb.AppendLine("      \"Token\": \"YOUR_BOT_TOKEN_HERE\",");
        sb.AppendLine("      \"AllowedUpdates\": [\"message\", \"callback_query\"],");
        sb.AppendLine("      \"RateLimit\": {");
        sb.AppendLine("        \"GlobalPerSecond\": 30,");
        sb.AppendLine("        \"PerChatPerSecond\": 1,");
        sb.AppendLine("        \"GroupPerMinute\": 20");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("// ============ Folder Structure ============");
        sb.AppendLine("// Handlers/     — command, callback, text, media handlers");
        if (useTemplates)
            sb.AppendLine("// Templates/    — YAML template files");
        if (usePayments)
            sb.AppendLine("// Payments/     — payment validators, processors, refund processors");
        sb.AppendLine("// Middleware/   — custom middleware (optional)");

        return sb.ToString();
    }
}
