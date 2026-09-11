using System.ComponentModel;
using ModelContextProtocol.Server;

namespace TeleForge.Mcp.Tools;

[McpServerToolType]
public static class QueryTools
{
    [McpServerTool]
    [Description(
        "Returns a complete reference of all TeleForge handler interfaces, their context classes, attributes, and result types. Use this to understand what handler types are available and how to implement them.")]
    public static string ListHandlerTypes()
    {
        return """
               # TeleForge Handler Types

               ## 1. ICommandHandler — handles /command messages
               - Interface: `ICommandHandler`
               - Method: `HandleAsync(CommandContext context, CancellationToken ct) → Task<CommandResult>`
               - Attribute: `[TelegramCommand("commandname", Description = "...")]`
                 - Properties: Command, Description, AllowedChatTypes, RequiresUserAccount, RequiredArguments, UsageExample
               - Context: `CommandContext`
                 - Properties: Command (string), Arguments (string[]), RawText (string), ChatId, UserId, BotId, CancellationToken, RequestServices
               - Result: `CommandResult`
                 - Static: `CommandResult.Ok()`, `CommandResult.Fail("error")`

               ## 2. ICallbackQueryHandler — handles inline button presses
               - Interface: `ICallbackQueryHandler`
               - Method: `HandleAsync(CallbackContext context, CancellationToken ct) → Task<CallbackResult>`
               - Attribute: `[CallbackQuery("pattern", Description = "...")]`
                 - Properties: Pattern (supports route params like `action:{id}` or regex with IsRegex=true), Description, IsRegex
               - Context: `CallbackContext`
                 - Properties: CallbackData (string), CallbackQueryId (string), RouteParameters (IDictionary<string,string>), ChatId, UserId, BotId
                 - Methods: `GetRouteParam<T>("name")`, `GetRouteParam("name")`
               - Result: `CallbackResult`
                 - Static: `CallbackResult.Ok()`, `CallbackResult.Alert("text", showAlert)`, `CallbackResult.Fail("error")`

               ## 3. ITextMessageHandler — handles plain text messages
               - Interface: `ITextMessageHandler`
               - Method: `HandleAsync(TextMessageContext context, CancellationToken ct) → Task<TextMessageResult>`
               - Attribute: `[TextMessage(pattern)]` (optional regex pattern)
                 - Properties: Pattern (string?), Priority (int, higher = checked first)
               - Context: `TextMessageContext`
                 - Properties: Text (string), ChatId, UserId, BotId, CancellationToken, RequestServices
               - Result: `TextMessageResult`
                 - Static: `TextMessageResult.Ok()`, `TextMessageResult.Fail("error")`

               ## 4. IMediaHandler — handles media messages (photo, video, document, etc.)
               - Interface: `IMediaHandler`
               - Method: `HandleAsync(MediaContext context, CancellationToken ct) → Task`
               - Attribute: none required (implicit discovery)
               - Context: `MediaContext`
                 - Properties: Message (Telegram.Bot Message), MediaType (Photo/Video/Audio/Document/Voice/VideoNote/Sticker/Animation), Caption (string?), ChatId, UserId, BotId

               ## 5. IInlineQueryHandler — handles inline queries
               - Interface: `IInlineQueryHandler`
               - Method: `HandleAsync(InlineQueryContext context, CancellationToken ct) → Task`
               - Attribute: none required
               - Context: `InlineQueryContext`
                 - Properties: InlineQuery, Query (string), Offset (string?), UserId, BotId

               ## 6. IDiceHandler — handles dice/game results
               - Interface: `IDiceHandler`
               - Method: `HandleAsync(DiceContext context, CancellationToken ct) → Task`

               ## 7. ILocationHandler — handles location messages
               ## 8. IContactHandler — handles contact shares
               ## 9. IPollAnswerHandler — handles poll answers
               ## 10. IChatMemberHandler — handles chat member updates
               ## 11. IChosenInlineResultHandler — handles chosen inline results

               ## 12. IConversationHandler — multi-step conversation flows
               - Interface: `IConversationHandler`
               - Method: `HandleStepAsync(ConversationStepContext context, CancellationToken ct) → Task<ConversationResult>`
               - Context: `ConversationStepContext`
                 - Properties: UpdateContext, Step (ConversationStep), Input (string), ChatId, UserId, BotId, RequestServices
                 - Methods: `GetStepData("key")`
               - Result: `ConversationResult`
                 - Static: `ConversationResult.Complete()`, `ConversationResult.GoTo(nextStep)`, `ConversationResult.Invalid("error")`
               - Step: `ConversationStep { StepId, HandlerType, Data, ExpiresAt }`

               ## Cross-cutting attributes (apply to any handler):
               - `[Authorize("policyName")]` — require authorization policy
               - `[RateLimit(seconds)]` — per-user cooldown
               - `[ChatType("private", "group")]` — restrict to chat types
               """;
    }

    [McpServerTool]
    [Description(
        "Returns the complete ITelegramMessage fluent builder API. Use this to understand how to construct and send messages, edit messages, attach keyboards, schedule delivery, etc.")]
    public static string ListMessageBuilderApi()
    {
        return """
               # ITelegramMessage Fluent Builder API

               Create via: `ITelegramMessageService.CreateMessage("botId")`

               ## Target
               - `.ToChat(long chatId)` — set target chat
               - `.InThread(int threadId)` — set target thread/topic

               ## Content — via template
               - `.WithTemplate("templateName")` — use YAML template
               - `.WithParameters(IDictionary<string, string>)` — template parameters
               - `.WithParameter("key", "value")` — single parameter
               - `.WithLanguage("en")` — rendering language

               ## Content — via direct text
               - `.WithText("text")` — plain text
               - `.WithHtml("<b>text</b>")` — HTML formatted
               - `.WithParseMode(TelegramParseMode.Html)` — set parse mode

               ## Media
               - `.WithMedia(TelegramMediaType.Photo, "url", "caption")` — attach media

               ## Inline Keyboard — from template dynamic slots
               - `.WithKeyboardItems("slotName", items, page)` — dynamic keyboard data
                 items: `IReadOnlyList<KeyboardItemData>`, page: pagination page number

               ## Inline Keyboard — raw
               - `.WithInlineKeyboard(InlineKeyboardMarkup keyboard)` — raw Telegram keyboard

               ## Reply Keyboard
               - `.WithReplyKeyboard(rows, resize, oneTime, placeholder)` — custom reply keyboard
               - `.WithForceReply("placeholder")` — force user to reply
               - `.RemoveReplyKeyboard()` — remove reply keyboard

               ## Edit Mode
               - `.EditMessage(int messageId)` — edit existing message text + keyboard
               - `.EditKeyboardOnly(int messageId)` — edit only the keyboard

               ## Delivery Options
               - `.WithPriority(MessagePriority.High)` — Low/Normal/High/Critical
               - `.BypassRateLimit()` — skip rate limiting for this message
               - `.DisableNotification()` — silent message
               - `.ProtectContent()` — prevent forwarding/saving
               - `.ScheduleAt(DateTimeOffset)` — delayed delivery

               ## Send
               - `.SendAsync(ct)` → `Task<SendResult>` — send immediately through pipeline
               - `.QueueAsync(ct)` → `Task<SendResult>` — enqueue for background delivery
               - `.SendOrQueueAsync(ct)` → `Task<SendResult>` — send if possible, queue if rate-limited
               - `.Build()` → `QueuedTelegramMessage` — build message object for external use

               ## SendResult
               - `Success: bool`, `MessageId: int?`, `ChatId: long?`
               - `Error: string?`, `ErrorCode: int?`, `RetryAfter: TimeSpan?`
               - Static: `SendResult.Ok(chatId, messageId)`, `SendResult.Failed(error, code)`, `SendResult.RateLimited(retryAfter)`

               ## Batch API
               ```csharp
               var batch = messageService.CreateBatch("botId");
               batch.Add(msg1).Add(msg2);
               IReadOnlyList<SendResult> results = await batch.SendAsync(ct);
               ```
               """;
    }

    [McpServerTool]
    [Description(
        "Returns the complete YAML template syntax reference for TeleForge templates — structure, translations, mustache variables, conditionals, loops, formatters, partials, layouts, static/dynamic buttons, pagination, emoji.")]
    public static string ListTemplateSyntax()
    {
        return """
               # TeleForge Template Syntax (YAML)

               ## File Structure
               ```yaml
               name: welcome_message
               category: greetings          # optional grouping
               parseMode: Html              # None | Html | Markdown | MarkdownV2
               text:
                 en: "Hello, <b>{{username}}</b>! Welcome."
                 ru: "Привет, <b>{{username}}</b>! Добро пожаловать."
               ```

               ## Mustache Variables
               - `{{variable}}` — replaced with parameter value
               - `{{variable|format}}` — apply custom formatter (e.g., `{{price|currency}}`)

               ## Conditionals
               ```yaml
               text:
                 en: |
                   {{#if isPremium}}
                   ⭐ Premium member! Your features:
                   {{/if}}
                   {{#unless isPremium}}
                   Upgrade to premium for more features.
                   {{/unless}}
               ```

               ## Loops
               ```yaml
               text:
                 en: |
                   Your items:
                   {{#each items}}
                   - {{name}}: {{price}}
                   {{/each}}
               ```

               ## Partials (reusable template fragments)
               ```yaml
               name: _footer
               isPartial: true
               text:
                 en: "---\nPowered by TeleForge"
               ```
               Include: `{{> _footer}}`

               ## Layout Inheritance
               ```yaml
               name: shop_layout
               isLayout: true
               text:
                 en: |
                   🏪 Shop
                   {{@content}}
               blocks:
                 footer:
                   en: "Use /help for assistance"
               ```
               Child: `extends: shop_layout`

               ## Static Buttons
               ```yaml
               buttons:
                 - text:
                     en: "✅ Confirm"
                     ru: "✅ Подтвердить"
                   type: Callback           # Callback | Url | WebApp | SwitchInline | Pay | Login
                   value: "confirm:{{orderId}}"
                   row: 0
                   order: 0
                   showIf: "hasOrder"       # conditional rendering (parameter must be truthy)
                 - text:
                     en: "❌ Cancel"
                   type: Callback
                   value: "cancel:{{orderId}}"
                   row: 0
                   order: 1
               ```

               ## Dynamic Buttons (data-driven, paginated)
               ```yaml
               dynamicButtons:
                 - name: products           # slot name — referenced in .WithKeyboardItems("products", items, page)
                   text:
                     en: "{{productName}} — {{price}}⭐"
                   type: Callback
                   value: "buy:{{productId}}"
                   rowStart: 1
                   itemsPerRow: 1
                   itemsPerPage: 5
                   paginationCallbackPrefix: "products_page"
                   paginationRow: 6
                   paginationButtons:
                     previous:
                       en: "⬅️ Back"
                     next:
                       en: "➡️ Next"
                     counter: "{{current}}/{{total}}"
               ```
               KeyboardItemData: `KeyboardItemData.FromValues(("productName", "Sword"), ("price", "10"), ("productId", "1"))`

               ## Emoji
               - In text: `:emoji_name:` → replaced via IEmojiRegistry
               - Registry loaded from YAML file configured in `TelegramTemplateOptions.EmojiFile`
               - Custom: `emojiRegistry.Register("fire", "🔥")`

               ## Registration
               ```csharp
               services.AddTelegramTemplates(options =>
               {
                   options.Directories.Add("Templates");
                   options.DefaultLanguage = "ru";
                   options.FallbackLanguage = "en";
                   options.EmojiFile = "Templates/emoji.yml";
                   options.HotReload = true;
                   options.AddFormatter<CurrencyFormatter>();
               });
               ```
               """;
    }

    [McpServerTool]
    [Description(
        "Returns the complete payment system API reference — IInvoiceBuilder, payment processing, refunds, gifts, subscriptions, paid media, TypedPayload pattern, and registration.")]
    public static string ListPaymentApi()
    {
        return """
               # TeleForge Payment API

               ## Registration
               ```csharp
               services.AddTelegramPayments(
                   options =>
                   {
                       options.AutoRefundOnFailure = true;
                       options.PayoutBatchSize = 10;
                       options.PayoutMaxRetries = 3;
                   },
                   handlers =>
                   {
                       handlers.AddValidator<ShopCheckoutValidator>("shop");
                       handlers.AddProcessor<ShopPaymentProcessor>("shop");
                       handlers.AddRefundProcessor<ShopRefundProcessor>("shop");
                   });
               ```

               ## Invoice Builder (fluent)
               ```csharp
               IInvoiceBuilder builder = invoiceService.CreateBuilder()
                   .WithTitle("Premium Subscription")
                   .WithDescription("Monthly premium access")
                   .WithPayload(new ShopPayload("premium_monthly"))  // TypedPayload
                   .AddPrice("Monthly fee", 100)                     // in smallest units (Stars)
                   .WithMaxTipAmount(50)
                   .WithSuggestedTips(10, 25, 50)
                   .AsType(InvoiceType.Subscription)
                   .WithSubscriptionPeriod(2592000);                 // 30 days in seconds

               InvoiceDefinition invoice = builder.Build();
               InvoiceResult result = await invoiceService.SendInvoiceAsync(chatId, invoice, "botId", ct);
               // or: string link = (await invoiceService.CreateLinkAsync(invoice, "botId", ct)).InvoiceLink;
               ```

               ## TypedPayload Pattern
               ```csharp
               public class ShopPayload : TypedPayload
               {
                   public override string Prefix => "shop";
                   public string ItemId { get; }
                   public ShopPayload(string itemId) => ItemId = itemId;
                   public override string[] SerializeFields() => [ItemId];
               }
               ```
               Prefix routes pre-checkout/payment to correct validator/processor.

               ## Pre-Checkout Validator
               ```csharp
               public class ShopCheckoutValidator : IPreCheckoutValidator
               {
                   public async Task<CheckoutValidationResult> ValidateAsync(PreCheckoutContext context, CancellationToken ct)
                   {
                       var payload = context.GetPayload<ShopPayload>(fields => new ShopPayload(fields[0]));
                       // validate stock, user eligibility, etc.
                       return CheckoutValidationResult.Approve();
                       // or: CheckoutValidationResult.Reject("out_of_stock", "Item is out of stock");
                   }
               }
               ```

               ## Payment Processor
               ```csharp
               public class ShopPaymentProcessor : IPaymentProcessor
               {
                   public async Task ProcessAsync(SuccessfulPaymentContext context, CancellationToken ct)
                   {
                       var payload = context.GetPayload<ShopPayload>(fields => new ShopPayload(fields[0]));
                       // grant item, update database, send confirmation
                   }
               }
               ```

               ## Refund Processor
               ```csharp
               public class ShopRefundProcessor : IRefundProcessor
               {
                   public async Task<RefundDecision> ProcessAsync(RefundContext context, CancellationToken ct)
                   {
                       return RefundDecision.Approve(RefundReason.UserRequest);
                   }
               }
               ```

               ## Manual Refund
               ```csharp
               bool success = await refundService.RefundStarPaymentAsync("botId", userId, chargeId, ct);
               ```

               ## Gift Service
               ```csharp
               var gifts = await giftService.GetAvailableGiftsAsync("botId", ct);
               await giftService.SendGiftAsync(new SendGiftRequest { BotId = "bot", UserId = 123, GiftId = "gift_1" }, ct);
               await giftService.ConvertToStarsAsync("botId", userId, ownedGiftId, ct);
               await giftService.GiftPremiumAsync("botId", userId, months: 3, ct);
               ```

               ## Subscription Service
               ```csharp
               var link = await subscriptionService.CreateSubscriptionLinkAsync(
                   new SubscriptionPlan { Title = "VIP", Price = 50, Period = 2592000, Currency = PaymentCurrency.Xtr }, ct);
               ```

               ## Paid Media
               ```csharp
               await paidMediaService.SendPaidMediaAsync(new PaidMediaDefinition
               {
                   StarCount = 10,
                   Input_media = [new InputMediaPhoto("file_id")],
                   PayloadPrefix = "exclusive"
               }, ct);
               ```

               ## Star Transactions
               ```csharp
               StarBalance balance = await starService.GetBalanceAsync("botId", ct);
               var transactions = await starService.GetTransactionsAsync("botId", offset: 0, limit: 50, ct);
               ```

               ## Payment Flow
               1. Create invoice → send to user
               2. User clicks Pay → Telegram sends PreCheckoutQuery
               3. PreCheckoutPipeline routes to validator by payload prefix → Approve/Reject
               4. User confirms → Telegram sends SuccessfulPayment
               5. PaymentProcessingPipeline routes to processor by prefix
               6. On failure + AutoRefundOnFailure=true → automatic refund
               """;
    }

    [McpServerTool]
    [Description(
        "Returns all DI registration extension methods for TeleForge — AddTelegramRouting, AddTelegramMessaging, AddTelegramTemplates, AddTelegramConsumer, AddTelegramPayments — with their full options.")]
    public static string ShowRegistrationPatterns()
    {
        return """
               # TeleForge DI Registration Patterns

               ## 1. Routing
               ```csharp
               services.AddTelegramRouting(options =>
               {
                   options.DiscoveryMode = HandlerDiscoveryMode.Reflection; // or SourceGenerator
                   options.HandlerAssemblies.Add(typeof(MyHandler).Assembly);
                   options.PolicyConfigurations["admin"] = policy =>
                       policy.RequireRank("admin", "owner");
                   options.PolicyConfigurations["premium"] = policy =>
                       policy.RequirePermission("premium_access");
               });
               ```
               Registers: IHandlerRegistry, ReflectionHandlerDiscovery, ICallbackDataSerializer,
               IConversationStateStore (default: in-memory), INavigationService (default: in-memory),
               IUserLocaleResolver (default: Telegram language), ITelegramUpdatePipeline

               ## 2. Messaging
               ```csharp
               services.AddTelegramMessaging(options =>
               {
                   // Optional custom backends:
                   options.RateLimitStoreType = typeof(RedisRateLimitStore);
                   options.QueueBackendType = typeof(MassTransitQueueBackend);
               });
               ```
               Registers: ITelegramBotClientProvider, ITelegramMessageService, ISendPipeline,
               IRateLimitStore, IMessageQueueBackend (if configured)
               Send middleware chain: RateLimit → Retry → CircuitBreaker → Metrics → Transport

               ## 3. Templates
               ```csharp
               services.AddTelegramTemplates(options =>
               {
                   options.Directories.Add("Templates");
                   options.DefaultLanguage = "ru";
                   options.FallbackLanguage = "en";
                   options.EmojiFile = "Templates/emoji.yml";
                   options.HotReload = true;     // reload on file change
                   options.PreCompile = true;    // compile on startup
                   options.AddFormatter<CurrencyFormatter>();
               });
               ```
               Registers: IEmojiRegistry, IMessageTemplateStore, ITemplateRenderer,
               IKeyboardBuilder, IMessageRenderer

               ## 4. Consumer
               ```csharp
               services.AddTelegramConsumer(options =>
               {
                   options.DefaultTransport = UpdateTransport.LongPolling; // or Webhook, Queue
                   options.ConcurrencyLimit = 50;
                   options.ChannelCapacity = 1000;
                   options.GracefulShutdownTimeout = TimeSpan.FromSeconds(30);
                   options.PollingTimeoutSeconds = 30;
                   options.PollingLimit = 100;
                   options.ConfigureWebhook(webhook =>
                   {
                       webhook.Path = "/api/telegram/webhook/{botId}";
                       webhook.SecretToken = "my-secret";
                       webhook.MaxConnections = 40;
                       webhook.DropPendingUpdates = false;
                   });
                   options.EnableHealthChecks(health =>
                   {
                       health.PollTimeoutThreshold = TimeSpan.FromMinutes(5);
                       health.UnhealthyAfterConsecutiveErrors = 10;
                   });
               });
               ```
               Registers: TelegramConsumerHostedService (IHostedService), TelegramUpdateWorker,
               LongPollingConsumer, WebhookUpdateHandler

               ## 5. Payments
               ```csharp
               services.AddTelegramPayments(
                   options =>
                   {
                       options.AutoRefundOnFailure = true;
                       options.PayoutWorkerInterval = TimeSpan.FromSeconds(30);
                       options.PayoutBatchSize = 10;
                       options.PayoutMaxRetries = 3;
                       options.RevenueSyncEnabled = false;
                   },
                   handlers =>
                   {
                       handlers.AddValidator<MyValidator>("prefix");
                       handlers.AddProcessor<MyProcessor>("prefix");
                       handlers.AddRefundProcessor<MyRefundProcessor>("prefix");
                   });
               ```
               Registers: IInvoiceService, IPaymentRouter, IRefundService,
               IStarTransactionService, IGiftService, ISubscriptionService, IPaidMediaService

               ## Typical Program.cs
               ```csharp
               var builder = Host.CreateApplicationBuilder(args);

               builder.Services.AddTelegramMessaging(msg => { });
               builder.Services.AddTelegramRouting(routing =>
               {
                   routing.HandlerAssemblies.Add(typeof(Program).Assembly);
               });
               builder.Services.AddTelegramTemplates(tmpl =>
               {
                   tmpl.DefaultLanguage = "en";
               });
               builder.Services.AddTelegramConsumer(consumer =>
               {
                   consumer.DefaultTransport = UpdateTransport.LongPolling;
               });
               // Optional:
               // builder.Services.AddTelegramPayments(...);

               await builder.Build().RunAsync();
               ```

               ## Bot Configuration (appsettings.json)
               ```json
               {
                 "Bots": [
                   {
                     "Key": "main",
                     "Token": "BOT_TOKEN_HERE",
                     "AllowedUpdates": ["message", "callback_query"],
                     "Transport": "LongPolling",
                     "ConcurrencyLimit": 50,
                     "RateLimit": {
                       "GlobalPerSecond": 30,
                       "PerChatPerSecond": 1,
                       "GroupPerMinute": 20
                     }
                   }
                 ]
               }
               ```
               """;
    }
}
