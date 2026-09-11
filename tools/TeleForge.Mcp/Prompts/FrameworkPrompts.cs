using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace TeleForge.Mcp.Prompts;

[McpServerPromptType]
public class FrameworkPrompts
{
    [McpServerPrompt]
    [Description(
        "Guides AI to create a correct TeleForge handler with full framework context, conventions, and the user's requirements.")]
    public static IEnumerable<ChatMessage> CreateHandler(
        [Description("Handler type: command, callback, text, media, inline, dice, conversation")]
        string handlerType,
        [Description("What the handler should do — describe the behavior")]
        string description)
    {
        return
        [
            new ChatMessage(ChatRole.System, $"""
                                              You are creating a TeleForge handler. Follow these conventions strictly:

                                              HANDLER CONVENTIONS:
                                              - Each handler is a separate class implementing one handler interface
                                              - Use constructor injection for services (ITelegramMessageService, etc.)
                                              - Use the correct attribute for the handler type
                                              - Return the correct result type using static factory methods
                                              - Use context properties (ChatId, UserId, BotId) — never access RawUpdate directly
                                              - Use ITelegramMessageService fluent builder for sending messages
                                              - Add [Authorize], [RateLimit], [ChatType] attributes as appropriate

                                              HANDLER TYPE: {handlerType}

                                              Key signatures per type:
                                              - command: ICommandHandler, [TelegramCommand("name")], HandleAsync(CommandContext, CT) → Task<CommandResult>
                                              - callback: ICallbackQueryHandler, [CallbackQuery("pattern")], HandleAsync(CallbackContext, CT) → Task<CallbackResult>
                                              - text: ITextMessageHandler, [TextMessage(pattern?)], HandleAsync(TextMessageContext, CT) → Task<TextMessageResult>
                                              - media: IMediaHandler, HandleAsync(MediaContext, CT) → Task
                                              - inline: IInlineQueryHandler, HandleAsync(InlineQueryContext, CT) → Task
                                              - conversation: IConversationHandler, HandleStepAsync(ConversationStepContext, CT) → Task<ConversationResult>
                                              """),
            new ChatMessage(ChatRole.User, $"Create a {handlerType} handler that: {description}")
        ];
    }

    [McpServerPrompt]
    [Description(
        "Guides AI to create a YAML template file for TeleForge with proper syntax, translations, and keyboard configuration.")]
    public static IEnumerable<ChatMessage> CreateTemplate(
        [Description("What the template should display — describe the message")]
        string description,
        [Description("Comma-separated language codes (e.g., 'en,ru')")]
        string languages = "en")
    {
        var systemPrompt = """
                           You are creating a TeleForge YAML template. Follow this syntax:

                           STRUCTURE:
                           ```yaml
                           name: template_name
                           parseMode: Html
                           text:
                             en: |
                               Text with {{variable}} placeholders
                               {{#if condition}}conditional{{/if}}
                               {{#each items}}loop item: {{name}}{{/each}}
                           buttons:
                             - text:
                                 en: "Button text"
                               type: Callback
                               value: "action:{{id}}"
                               row: 0
                               order: 0
                               showIf: "conditionParam"
                           dynamicButtons:
                             - name: slotName
                               text:
                                 en: "{{itemName}}"
                               type: Callback
                               value: "select:{{itemId}}"
                               rowStart: 1
                               itemsPerRow: 1
                               itemsPerPage: 5
                               paginationCallbackPrefix: "page_prefix"
                           ```

                           RULES:
                           - Every text block must have translations for ALL requested languages: LANGUAGES
                           - Button types: Callback, Url, WebApp, SwitchInline, Pay, Login
                           - Use parseMode: Html for <b>bold</b>, <i>italic</i>, <code>code</code>
                           - Use :emoji_name: for emoji injection
                           - Dynamic button slots are referenced in code via .WithKeyboardItems("slotName", items, page)
                           """.Replace("LANGUAGES", languages);

        return
        [
            new ChatMessage(ChatRole.System, systemPrompt),
            new ChatMessage(ChatRole.User, $"Create a template for: {description}\nLanguages: {languages}")
        ];
    }

    [McpServerPrompt]
    [Description(
        "Guides AI to migrate custom Telegram bot code to TeleForge framework — analyzes source code and produces mapped handler/messaging code.")]
    public static IEnumerable<ChatMessage> MigrateFile(
        [Description("The source code of the custom bot file to migrate")]
        string sourceCode)
    {
        return
        [
            new ChatMessage(ChatRole.System, """
                                             You are migrating custom Telegram bot code to the TeleForge framework.

                                             MAPPING RULES:
                                             1. RAW API CALLS → ITelegramMessageService fluent builder:
                                                - botClient.SendTextMessageAsync(chatId, text) → messageService.CreateMessage("botId").ToChat(chatId).WithText(text).SendAsync(ct)
                                                - botClient.SendPhotoAsync(...) → .WithMedia(TelegramMediaType.Photo, url, caption)
                                                - botClient.EditMessageTextAsync(...) → .EditMessage(messageId).WithText(text).SendAsync(ct)
                                                - botClient.AnswerCallbackQueryAsync(...) → return CallbackResult.Alert("text")

                                             2. UPDATE HANDLING → Handler classes:
                                                - if (update.Message?.Text?.StartsWith("/")) → ICommandHandler + [TelegramCommand]
                                                - if (update.CallbackQuery != null) → ICallbackQueryHandler + [CallbackQuery]
                                                - switch on callback data → separate ICallbackQueryHandler per pattern
                                                - plain text handling → ITextMessageHandler + [TextMessage]
                                                - photo/video/document → IMediaHandler

                                             3. STATE MANAGEMENT:
                                                - Dictionary<long, UserState> → IConversationHandler + IConversationStateStore
                                                - Step-based flows → ConversationStep with GoTo/Complete/Invalid results

                                             4. KEYBOARD CONSTRUCTION:
                                                - new InlineKeyboardMarkup(buttons) → YAML template with buttons/dynamicButtons
                                                - InlineKeyboardButton.WithCallbackData → template button with type: Callback

                                             5. BOT CONFIGURATION:
                                                - new TelegramBotClient(token) → BotConfiguration in appsettings.json + ITelegramBotClientProvider

                                             OUTPUT FORMAT:
                                             - For each identified pattern, generate the TeleForge equivalent
                                             - Show the new handler class(es) with correct interfaces and attributes
                                             - Show any needed YAML templates
                                             - Show any needed DI registration changes
                                             """),
            new ChatMessage(ChatRole.User, $"Migrate this code to TeleForge:\n\n```csharp\n{sourceCode}\n```")
        ];
    }

    [McpServerPrompt]
    [Description(
        "Guides AI to implement a complete payment flow in TeleForge — invoice creation, checkout validation, payment processing, refunds.")]
    public static IEnumerable<ChatMessage> AddPaymentFlow(
        [Description("Describe the payment scenario (e.g., 'in-app shop for virtual items', 'premium subscription')")]
        string description)
    {
        return
        [
            new ChatMessage(ChatRole.System, """
                                             You are implementing a payment flow in TeleForge. Follow this architecture:

                                             COMPONENTS NEEDED:
                                             1. TypedPayload class — defines payload prefix and serialized fields
                                             2. IPreCheckoutValidator — validates pre-checkout (stock, eligibility, limits)
                                             3. IPaymentProcessor — processes successful payment (grant item, update DB)
                                             4. IRefundProcessor — handles refund requests (optional)
                                             5. Invoice creation — using IInvoiceBuilder fluent chain
                                             6. Command/callback handler — triggers invoice sending

                                             TYPED PAYLOAD PATTERN:
                                             ```csharp
                                             public class MyPayload : TypedPayload
                                             {
                                                 public override string Prefix => "my_prefix";
                                                 public string ItemId { get; }
                                                 public MyPayload(string itemId) => ItemId = itemId;
                                                 public override string[] SerializeFields() => [ItemId];
                                             }
                                             ```

                                             INVOICE BUILDER:
                                             ```csharp
                                             var invoice = invoiceService.CreateBuilder()
                                                 .WithTitle("Title")
                                                 .WithDescription("Description")
                                                 .WithPayload(new MyPayload("item_1"))
                                                 .AddPrice("Item", 100)
                                                 .Build();
                                             await invoiceService.SendInvoiceAsync(chatId, invoice, "botId", ct);
                                             ```

                                             REGISTRATION:
                                             ```csharp
                                             services.AddTelegramPayments(
                                                 options => { options.AutoRefundOnFailure = true; },
                                                 handlers =>
                                                 {
                                                     handlers.AddValidator<MyValidator>("my_prefix");
                                                     handlers.AddProcessor<MyProcessor>("my_prefix");
                                                     handlers.AddRefundProcessor<MyRefundProcessor>("my_prefix");
                                                 });
                                             ```

                                             FLOW: User clicks Pay → PreCheckout (validate) → Payment (process) → Confirmation
                                             On failure with AutoRefund: automatic refund + RefundProcessor called
                                             """),
            new ChatMessage(ChatRole.User, $"Implement a payment flow for: {description}")
        ];
    }

    [McpServerPrompt]
    [Description(
        "Comprehensive bot setup guide — returns a prompt with all registration patterns, architecture layers, and conventions for setting up a new TeleForge bot from scratch.")]
    public static IEnumerable<ChatMessage> SetupBot()
    {
        return
        [
            new ChatMessage(ChatRole.System, """
                                             You are helping set up a new TeleForge bot project. Provide complete guidance.

                                             ARCHITECTURE (3 layers):
                                             1. Consumer layer — receives updates (LongPolling/Webhook/Queue)
                                             2. Routing layer — dispatches to handlers via attributes + middleware pipeline
                                             3. Messaging layer — sends messages via fluent builder + send pipeline (RateLimit→Retry→CircuitBreaker→Metrics→Transport)

                                             REQUIRED PACKAGES:
                                             - TeleForge.Consumer — update consumption
                                             - TeleForge.Routing — handler dispatch
                                             - TeleForge.Messaging — message sending
                                             - TeleForge.Templates — YAML template engine (recommended)
                                             - TeleForge.Payments — payment system (optional)
                                             - Microsoft.Extensions.Hosting — host infrastructure

                                             REGISTRATION ORDER (Program.cs):
                                             1. AddTelegramMessaging() — message service + send pipeline
                                             2. AddTelegramRouting() — handler registry + update pipeline
                                             3. AddTelegramTemplates() — template engine (if used)
                                             4. AddTelegramConsumer() — update consumer (starts polling/webhook)
                                             5. AddTelegramPayments() — payment system (if used)

                                             BOT CONFIG (appsettings.json):
                                             ```json
                                             {
                                               "Bots": [{
                                                 "Key": "main",
                                                 "Token": "BOT_TOKEN",
                                                 "AllowedUpdates": ["message", "callback_query"],
                                                 "RateLimit": { "GlobalPerSecond": 30, "PerChatPerSecond": 1, "GroupPerMinute": 20 }
                                               }]
                                             }
                                             ```

                                             HANDLER CONVENTIONS:
                                             - One handler per file, implements one interface
                                             - Constructor inject ITelegramMessageService + other services
                                             - Use attributes: [TelegramCommand], [CallbackQuery], [TextMessage], [Authorize], [RateLimit], [ChatType]
                                             - Return typed results: CommandResult.Ok(), CallbackResult.Alert(), etc.

                                             PROJECT STRUCTURE:
                                             MyBot/
                                             ├── Program.cs
                                             ├── appsettings.json
                                             ├── Handlers/
                                             ├── Templates/
                                             ├── Middleware/ (optional)
                                             └── Payments/ (optional)
                                             """),
            new ChatMessage(ChatRole.User,
                "Help me set up a new TeleForge bot project. Ask me what features I need and generate the complete setup.")
        ];
    }

    [McpServerPrompt]
    [Description(
        "Diagnoses routing and handler dispatch issues in TeleForge — common problems, checklist, and resolution steps.")]
    public static IEnumerable<ChatMessage> DebugRouting(
        [Description(
            "Describe the symptom (e.g., 'handler not found', 'callback not matching', 'command not responding')")]
        string symptom)
    {
        return
        [
            new ChatMessage(ChatRole.System, """
                                             You are diagnosing a TeleForge routing issue. Check these in order:

                                             COMMON ISSUES CHECKLIST:

                                             1. HANDLER NOT DISCOVERED:
                                                - Is the handler assembly registered? Check: options.HandlerAssemblies.Add(typeof(Handler).Assembly)
                                                - Does the class implement the correct interface? (ICommandHandler, ICallbackQueryHandler, etc.)
                                                - Is the correct attribute applied? (QG0001 analyzer warning)
                                                - Is the handler public and non-abstract?

                                             2. COMMAND NOT MATCHING:
                                                - Command name in [TelegramCommand("name")] must NOT include the "/" prefix
                                                - Check AllowedUpdates includes "message" in bot configuration
                                                - Check [ChatType] attribute isn't restricting to wrong chat type
                                                - Check [Authorize] policy isn't blocking the user

                                             3. CALLBACK NOT MATCHING:
                                                - Pattern in [CallbackQuery("pattern")] must match the callback_data sent by buttons
                                                - Route parameters: "action:{id}" matches "action:123" → id="123"
                                                - Regex patterns: set IsRegex=true on the attribute
                                                - Check for duplicate routes (QG0002 analyzer error)
                                                - Check AllowedUpdates includes "callback_query"

                                             4. TEXT HANDLER NOT FIRING:
                                                - [TextMessage] without pattern catches ALL text — add Priority to control order
                                                - Pattern is regex — ensure it matches the expected input
                                                - Conversation handlers take priority over text handlers if user is in active conversation

                                             5. MIDDLEWARE BLOCKING:
                                                - ConversationMiddleware intercepts updates for users in active conversations
                                                - Authorization middleware returns 403 before handler runs
                                                - Rate limit middleware returns cooldown before handler runs

                                             6. RATE LIMITING:
                                                - [RateLimit(seconds)] is per-user, per-handler
                                                - Transport rate limits (GlobalPerSecond, PerChatPerSecond) may delay message sending, not receiving

                                             ASK FOR:
                                             - Handler source code (class + attributes)
                                             - Registration code (Program.cs AddTelegramRouting section)
                                             - Bot configuration (appsettings.json AllowedUpdates)
                                             - Any middleware in the pipeline
                                             """),
            new ChatMessage(ChatRole.User, $"I'm having a routing issue: {symptom}")
        ];
    }
}
