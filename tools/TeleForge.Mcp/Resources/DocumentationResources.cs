using System.ComponentModel;
using ModelContextProtocol.Server;

namespace TeleForge.Mcp.Resources;

[McpServerResourceType]
public class DocumentationResources(WorkspaceContext workspace)
{
    [McpServerResource(UriTemplate = "docs://teleforge/architecture", Name = "Architecture", MimeType = "text/markdown")]
    [Description("TeleForge framework architecture overview — layers, pipeline, and design principles")]
    public string GetArchitecture()
    {
        return workspace.ReadDoc("architecture") ?? "Documentation file not found.";
    }

    [McpServerResource(UriTemplate = "docs://teleforge/routing", Name = "Routing", MimeType = "text/markdown")]
    [Description("Routing guide — handler types, attributes, authorization, middleware, handler discovery")]
    public string GetRouting()
    {
        return workspace.ReadDoc("routing") ?? "Documentation file not found.";
    }

    [McpServerResource(UriTemplate = "docs://teleforge/messaging", Name = "Messaging", MimeType = "text/markdown")]
    [Description("Messaging guide — ITelegramMessageService, fluent builder, send pipeline, queue backends")]
    public string GetMessaging()
    {
        return workspace.ReadDoc("messaging") ?? "Documentation file not found.";
    }

    [McpServerResource(UriTemplate = "docs://teleforge/templates", Name = "Templates", MimeType = "text/markdown")]
    [Description("Templates guide — YAML templates, translations, mustache syntax, keyboards, emoji")]
    public string GetTemplates()
    {
        return workspace.ReadDoc("templates") ?? "Documentation file not found.";
    }

    [McpServerResource(UriTemplate = "docs://teleforge/conversations", Name = "Conversations", MimeType = "text/markdown")]
    [Description("Conversations guide — multi-step handlers, ConversationStep, state stores")]
    public string GetConversations()
    {
        return workspace.ReadDoc("conversations") ?? "Documentation file not found.";
    }

    [McpServerResource(UriTemplate = "docs://teleforge/payments", Name = "Payments", MimeType = "text/markdown")]
    [Description("Payments guide — invoices, checkout, refunds, gifts, subscriptions, paid media")]
    public string GetPayments()
    {
        return workspace.ReadDoc("payments") ?? "Documentation file not found.";
    }

    [McpServerResource(UriTemplate = "docs://teleforge/rate-limiting", Name = "Rate Limiting", MimeType = "text/markdown")]
    [Description("Rate limiting guide — handler-level cooldowns, transport-level limits, bypass")]
    public string GetRateLimiting()
    {
        return workspace.ReadDoc("rate-limiting") ?? "Documentation file not found.";
    }

    [McpServerResource(UriTemplate = "docs://teleforge/migration", Name = "Migration", MimeType = "text/markdown")]
    [Description("Migration guide — migrating from other approaches to TeleForge")]
    public string GetMigration()
    {
        return workspace.ReadDoc("migration-from-tcl") ?? "Documentation file not found.";
    }

    [McpServerResource(UriTemplate = "docs://teleforge/analyzers", Name = "Analyzers", MimeType = "text/markdown")]
    [Description(
        "Roslyn analyzer rules — QG0001 MissingHandlerAttribute, QG0002 DuplicateRoute, QG0005 HandlerReturnType")]
    public static string GetAnalyzers()
    {
        return """
               # TeleForge Roslyn Analyzers

               ## QG0001 — MissingHandlerAttribute
               **Severity:** Warning
               **Trigger:** A class implements a handler interface but is missing the corresponding attribute.
               **Handler → Attribute mapping:**
               - ICommandHandler → [TelegramCommand]
               - ICallbackQueryHandler → [CallbackQuery]
               - ITextMessageHandler → [TextMessage]
               - IMediaHandler → (implicit, no attribute required)
               - IInlineQueryHandler → (implicit)
               - IDiceHandler → (implicit)
               - IConversationHandler → (implicit)

               ## QG0002 — DuplicateRoute
               **Severity:** Error
               **Trigger:** Two handlers register the same command name or overlapping callback pattern.

               ## QG0005 — HandlerReturnType
               **Severity:** Warning
               **Trigger:** Handler method returns wrong type (e.g., Task instead of Task<CommandResult>).
               """;
    }

    [McpServerResource(UriTemplate = "docs://teleforge/examples/{name}", Name = "Example Bot")]
    [Description("Source code of an example bot project — EchoBot, CommandBot, ConversationBot, etc.")]
    public string GetExample(string name)
    {
        return workspace.ReadExampleFiles(name) ??
               $"Example '{name}' not found. Available: EchoBot, CommandBot, ConversationBot, AdvancedBot, InlineBot, InlineKeyboardBot, MediaBot, MultiLanguageBot, DiceMiniGames, TelegramShop, WebhookBot";
    }
}
