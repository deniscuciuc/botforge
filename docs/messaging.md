# Messaging

## Registration

```csharp
builder.Services.AddTelegramMessaging(messaging =>
{
    messaging.AddBot("main", bot =>
    {
        bot.Token = "BOT_TOKEN";
    });

    // Optional: add send middleware
    messaging.UseSendMiddleware<AuditSendMiddleware>();

    // Optional: swap queue backend or rate limit store
    messaging.UseQueueBackend<MyQueueBackend>();
    messaging.UseRateLimitStore<MyRateLimitStore>();
});
```

## Configuration

### TelegramMessagingOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultPriority` | `MessagePriority` | `Normal` | Default message priority |
| `RetryCount` | `int` | `3` | Max retry attempts |
| `RetryBaseDelay` | `TimeSpan` | `1s` | Base delay for exponential backoff |
| `CircuitBreaker.FailureThreshold` | `int` | `10` | Failures before circuit opens |
| `CircuitBreaker.BreakDuration` | `TimeSpan` | `30s` | How long circuit stays open |

### BotConfiguration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Key` | `string` | required | Unique identifier |
| `Token` | `string` | required | Bot API token |
| `AllowedUpdates` | `string[]` | `[]` | Update types to receive |
| `DedicatedTransport` | `bool` | `false` | Separate HttpClient per bot |
| `ConcurrencyLimit` | `int` | `50` | Max concurrent sends |
| `Transport` | `UpdateTransport` | `LongPolling` | Polling or webhook |
| `RateLimit.GlobalPerSecond` | `int` | `30` | Bot-wide rate limit |
| `RateLimit.PerChatPerSecond` | `int` | `1` | Per-chat rate limit |
| `RateLimit.GroupPerMinute` | `int` | `20` | Group chat rate limit |

## Creating & Sending Messages

### Fluent Builder API

```csharp
// Inject ITelegramMessageService
await messaging.CreateMessage("main")
    .ToChat(chatId)
    .WithText("Hello!")
    .SendAsync(ct);
```

### All Builder Methods

| Category | Method | Description |
|----------|--------|-------------|
| **Target** | `ToChat(long chatId)` | Set destination chat |
| | `InThread(int threadId)` | Set forum topic thread |
| | `ReplyToMessage(int messageId)` | Send as a reply to an existing message |
| **Text** | `WithText(string text)` | Plain text content |
| | `WithHtml(string html)` | HTML content (auto-sets parse mode) |
| | `WithTextEntities(string text, IReadOnlyList<MessageEntity> entities)` | Send raw text with explicit Telegram text entities |
| | `WithParseMode(TelegramParseMode mode)` | Override parse mode |
| **Templates** | `WithTemplate(string name)` | Use a named template |
| | `WithParameters(IDictionary)` | Bulk template parameters |
| | `WithParameter(string key, string value)` | Single template parameter |
| | `WithLanguage(string lang)` | Language for template rendering |
| **Media** | `WithMedia(type, url, caption?)` | Attach media |
| | `WithMedia(type, bytes, fileName, caption?)` | Upload media bytes as an attachment |
| | `WithCaptionEntities(string caption, IReadOnlyList<MessageEntity> entities)` | Attach explicit caption entities for media |
| **Keyboards** | `WithInlineKeyboard(InlineKeyboardMarkup)` | Raw inline keyboard |
| | `WithKeyboardItems(slot, items, page)` | Dynamic template keyboard items |
| | `WithReplyKeyboard(rows, resize, oneTime, placeholder, isPersistent)` | Custom reply keyboard from text rows |
| | `WithReplyKeyboard(buttonRows, resize, oneTime, placeholder, isPersistent)` | Styled/request-capable reply keyboard |
| | `WithForceReply(placeholder?)` | Force user to reply |
| | `RemoveReplyKeyboard()` | Remove existing reply keyboard |
| **Edit** | `EditMessage(int messageId)` | Edit existing message text |
| | `EditKeyboardOnly(int messageId)` | Edit only the inline keyboard |
| **Delivery** | `WithPriority(MessagePriority)` | Set message priority |
| | `BypassRateLimit()` | Skip rate limiting |
| | `DisableNotification()` | Send silently |
| | `DisableLinkPreview()` | Disable link previews for text messages |
| | `ProtectContent()` | Prevent forwarding/saving |
| | `ShowCaptionAboveMedia()` | Show caption above supported media |
| | `AllowPaidBroadcast()` | Opt into Telegram paid high-throughput broadcast |
| | `WithMessageEffect(string)` | Attach a Telegram message effect in private chats |
| | `WithBusinessConnection(string)` | Send on behalf of a Telegram business connection |
| | `ScheduleAt(DateTimeOffset)` | Schedule for later |
| **Send** | `SendAsync(ct)` | Send through pipeline now |
| | `QueueAsync(ct)` | Enqueue to queue backend |
| | `SendOrQueueAsync(ct)` | Queue if backend exists, else send |
| **Build** | `Build()` | Materialize as `QueuedTelegramMessage` |

### Send Result

```csharp
SendResult result = await messaging.CreateMessage("main")
    .ToChat(chatId)
    .WithText("Hello!")
    .SendAsync(ct);

if (result.Success)
    Console.WriteLine($"Sent with ID {result.MessageId}");
else
    Console.WriteLine($"Error: {result.Error} (code {result.ErrorCode})");
```

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether send succeeded |
| `MessageId` | `int?` | Telegram message ID |
| `ChatId` | `long?` | Target chat ID |
| `Error` | `string?` | Error description |
| `ErrorCode` | `int?` | Telegram error code |
| `RetryAfter` | `TimeSpan?` | Retry delay (429 responses) |

## Batch Sending

```csharp
var batch = messaging.CreateBatch("main");

batch.Add(messaging.CreateMessage("main").ToChat(id1).WithText("A"));
batch.Add(messaging.CreateMessage("main").ToChat(id2).WithText("B"));

IReadOnlyList<SendResult> results = await batch.SendAsync(ct);
// or: await batch.QueueAsync(ct);
```

## Send Pipeline

Messages pass through a middleware pipeline before reaching the Telegram API:

```
RateLimitSendMiddleware
  → RetrySendMiddleware
    → CircuitBreakerSendMiddleware
      → MetricsSendMiddleware
        → TelegramApiTransport (terminal)
```

### Built-in Middleware

| Middleware | Purpose |
|-----------|---------|
| `RateLimitSendMiddleware` | Enforces global + per-chat rate limits. Respects `BypassRateLimit()`. |
| `RetrySendMiddleware` | Polly-based retry with exponential backoff + jitter. Retries on 429 and 5xx. |
| `CircuitBreakerSendMiddleware` | Per-bot circuit breaker. Opens after `FailureThreshold` consecutive failures. |
| `MetricsSendMiddleware` | Emits meter-based send counters/latency and logs at Debug level. |

See [Metrics and Observability](metrics.md) for metric names, labels, and Grafana dashboards.

### Custom Send Middleware

```csharp
public class AuditMiddleware : ISendMiddleware
{
    public async Task<SendResult> InvokeAsync(SendContext context, TelegramSendDelegate next)
    {
        // Before: inspect or modify message
        var chatId = context.Message.ChatId;
        
        var result = await next(context);

        // After: log result
        if (!result.Success)
            logger.LogWarning("Send failed for chat {ChatId}: {Error}", chatId, result.Error);

        return result;
    }
}

// Register
messaging.UseSendMiddleware<AuditMiddleware>();
```

### SendContext Properties

| Property | Type | Description |
|----------|------|-------------|
| `Message` | `QueuedTelegramMessage` | The message being sent |
| `Bot` | `BotConfiguration` | Bot configuration |
| `CancellationToken` | `CancellationToken` | Cancellation token |
| `Items` | `IDictionary<string, object>` | Arbitrary data bag |
| `Attempt` | `int` | Current attempt number (starts at 1) |

## Queue Backends

### In-Memory (Default)

Uses `System.Threading.Channels` with a bounded capacity of 10,000 messages and 4 consumer tasks. Suitable for single-process deployments.

```csharp
// Registered automatically when AddTelegramMessaging is called
// Messages queued via .QueueAsync() are processed in-process
```

### MassTransit (Distributed)

For multi-process or distributed deployments:

```csharp
// Replace in-memory backend
builder.Services.AddTelegramMassTransitBackend();

// In MassTransit bus configuration
builder.Services.AddMassTransit(bus =>
{
    bus.AddTelegramMessageConsumer();
    bus.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.ConfigureEndpoints(ctx);
    });
});
```

Messages are published to the message bus and consumed by `TelegramMessageConsumer`, which sends them through the normal `ISendPipeline`. Rate-limited messages trigger MassTransit delayed redelivery.

## Media Types

| Type | `TelegramMediaType` | Telegram Method |
|------|---------------------|-----------------|
| Photo | `Photo` | `SendPhoto` |
| Video | `Video` | `SendVideo` |
| Document | `Document` | `SendDocument` |
| Audio | `Audio` | `SendAudio` |
| Animation | `Animation` | `SendAnimation` |
| Sticker | `Sticker` | `SendSticker` |
| Voice | `Voice` | `SendVoice` |
| Video Note | `VideoNote` | `SendVideoNote` |

```csharp
await messaging.CreateMessage("main")
    .ToChat(chatId)
    .WithMedia(TelegramMediaType.Photo, "https://example.com/photo.jpg", "Caption")
    .SendAsync(ct);
```

```csharp
await messaging.CreateMessage("main")
    .ToChat(chatId)
    .ReplyToMessage(sourceMessageId)
    .WithMedia(TelegramMediaType.Document, reportBytes, "report.xlsx", "Run: <code>abc123</code>")
    .SendAsync(ct);
```

## Parse Modes

| Mode | `TelegramParseMode` | Description |
|------|---------------------|-------------|
| None | `None` | Plain text |
| HTML | `Html` | HTML formatting (default) |
| Markdown | `Markdown` | Legacy Markdown |
| MarkdownV2 | `MarkdownV2` | Telegram MarkdownV2 |

## Custom Emoji Entities

The transport now compiles Telegram custom emoji into first-class entities for outgoing text messages and media captions.

For plain text or captions, use Telegram's custom emoji tag directly:

```csharp
await messaging.CreateMessage("main")
        .ToChat(chatId)
        .WithHtml("<b>Premium</b> <tg-emoji emoji-id=\"5395601891781224729\">⭐</tg-emoji>")
        .SendAsync(ct);
```

If you need full control over entity ranges without markup, send the text and entities explicitly:

```csharp
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

await messaging.CreateMessage("main")
    .ToChat(chatId)
    .WithTextEntities(
        "Open docs",
        [
            new MessageEntity { Type = MessageEntityType.Bold, Offset = 0, Length = 4 },
            new MessageEntity
            {
                Type = MessageEntityType.TextLink,
                Offset = 5,
                Length = 4,
                Url = "https://core.telegram.org/bots/api"
            }
        ])
    .SendAsync(ct);
```

The same pattern works for media captions through `WithCaptionEntities(...)`.

## Rich Reply Keyboards

Reply keyboards now support Telegram button styles, premium custom emoji icons, and request-button variants such as contact, location, poll, user selection, chat selection, and Web Apps.

```csharp
using Telegram.Bot.Types.ReplyMarkups;

await messaging.CreateMessage("main")
    .ToChat(chatId)
    .WithText("Choose an action")
    .WithReplyKeyboard(
        [
            [new ReplyKeyboardButtonData
            {
                Text = "Share contact",
                Type = TelegramReplyButtonType.RequestContact,
                Style = TelegramButtonStyle.Primary,
                IconCustomEmojiId = "5395601891781224729"
            }],
            [new ReplyKeyboardButtonData
            {
                Text = "Choose moderators",
                Type = TelegramReplyButtonType.RequestUsers,
                RequestUsers = new KeyboardButtonRequestUsers(42)
                {
                    UserIsPremium = true,
                    MaxQuantity = 3,
                    RequestName = true
                }
            }]
        ],
        oneTime: true,
        isPersistent: true)
    .SendAsync(ct);
```

Rich reply keyboards are also forwarded for media sends, not only plain text messages.

For templates, the recommended path is still `{{emoji:name}}`, but the emoji registry value can now be a custom emoji tag with fallback content:

```yaml
Emojis:
    premium_star: '<tg-emoji emoji-id="5395601891781224729">⭐</tg-emoji>'
```

```yaml
Text:
    Translations:
        en: "{{emoji:premium_star}} Premium unlocked"
```

Notes:

- The transport compiles custom emoji tags into Telegram `CustomEmoji` entities for message text and captions.
- When custom emoji tags are used inside HTML messages, the transport converts the supported HTML subset into explicit Telegram entities so custom emoji and common formatting can coexist.
- Supported HTML tags in this path: `b`/`strong`, `i`/`em`, `u`/`ins`, `s`/`strike`/`del`, `code`, `pre`, `a`, `tg-spoiler`, `blockquote`, `tg-emoji`, and `br`.

## Message Priority

| Priority | Value | Description |
|----------|-------|-------------|
| `Low` | 0 | Batch notifications |
| `Normal` | 1 | Default |
| `High` | 2 | Important messages |
| `Critical` | 3 | System alerts |
