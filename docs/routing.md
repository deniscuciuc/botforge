# Routing & Handlers

## Handler Registration

Register handlers from an assembly:

```csharp
builder.Services.AddTelegramRouting(routing =>
{
    routing.AddHandlersFromAssembly(typeof(Program).Assembly);
});
```

## Command Handlers

```csharp
[TelegramCommand("/greet", "Greet a user")]
public class GreetHandler(ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var name = context.Arguments.Length > 0 ? context.Arguments[0] : "World";

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText($"Hello, {name}!")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}
```

### CommandContext Properties

| Property | Type | Description |
|----------|------|-------------|
| `UpdateContext` | `TelegramUpdateContext` | Raw update, bot key, services |
| `Command` | `string` | Command name (e.g., "/greet") |
| `Arguments` | `string[]` | Space-separated arguments after command |
| `RawText` | `string` | Full message text |
| `ChatId` | `long?` | Chat ID |
| `UserId` | `long?` | User ID |
| `BotId` | `string` | Bot key that received the update |
| `CancellationToken` | `CancellationToken` | Cancellation token |
| `RequestServices` | `IServiceProvider` | Scoped DI container |

### CommandResult

- `CommandResult.Ok()` — Success
- `CommandResult.Fail(error)` — Failure with error message

## Callback Query Handlers

```csharp
[CallbackQuery("product:{id}")]
public class ProductHandler(ITelegramMessageService messaging) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var productId = context.GetRouteParam<int>("id");
        // ...
        return CallbackResult.Ok();
    }
}
```

### Route Parameters

Callback patterns support `{param}` placeholders:
- `[CallbackQuery("page:{num}")]` → `context.GetRouteParam<int>("num")`
- `[CallbackQuery("action:{type}:{id}")]` → multiple params
- `[CallbackQuery("search:*", IsRegex = true)]` → regex matching

### CallbackContext Properties

| Property | Type | Description |
|----------|------|-------------|
| `CallbackData` | `string` | Full callback data string |
| `CallbackQueryId` | `string` | Query ID for answering |
| `RouteParameters` | `IDictionary<string, string>` | Extracted route params |
| `ChatId` | `long?` | Chat ID |
| `UserId` | `long?` | User ID |
| `BotId` | `string` | Bot key |

### CallbackResult

- `CallbackResult.Ok()` — Acknowledge
- `CallbackResult.Alert(text, showAlert)` — Show alert/toast
- `CallbackResult.Fail(error)` — Error

## Text Message Handlers

```csharp
[TextMessage(@"\d+")]  // Regex pattern
public class NumberHandler : ITextMessageHandler
{
    public async Task<TextMessageResult> HandleAsync(TextMessageContext context, CancellationToken ct)
    {
        var text = context.Text;
        return TextMessageResult.Ok();
    }
}
```

Use `[TextMessage]` without a pattern to match all text messages.

## Media Handlers

```csharp
[MediaMessage(MediaType.Photo)]
public class PhotoHandler : IMediaHandler
{
    public async Task HandleAsync(MediaContext context, CancellationToken ct)
    {
        var fileId = context.Message.Photo?.LastOrDefault()?.FileId;
    }
}
```

Supported types: `Photo`, `Document`, `Voice`, `Video`, `Audio`, `Sticker`, `VideoNote`, `Animation`.

## Service Message Handlers

```csharp
[ContactMessage]
public class ContactHandler : IContactHandler
{
    public Task HandleAsync(ContactContext context, CancellationToken ct)
    {
        var phone = context.PhoneNumber;
        return Task.CompletedTask;
    }
}

[UsersSharedMessage]
public class UsersSharedHandler : IUsersSharedHandler
{
    public Task HandleAsync(UsersSharedContext context, CancellationToken ct)
    {
        var requestId = context.RequestId;
        var users = context.Users;
        return Task.CompletedTask;
    }
}
```

Supported service-message attributes: `LocationMessage`, `ContactMessage`, `UsersSharedMessage`, `ChatSharedMessage`.

## Dice Handlers

```csharp
[DiceMessage("🎲")]  // Only dice emoji, or null for any
public class DiceHandler : IDiceHandler
{
    public async Task HandleAsync(DiceContext context, CancellationToken ct)
    {
        var value = context.Message.Dice?.Value;
    }
}
```

## Inline Query Handlers

```csharp
[InlineQuery]
public class SearchHandler : IInlineQueryHandler
{
    public async Task HandleAsync(InlineQueryContext context, CancellationToken ct)
    {
        var query = context.InlineQuery.Query;
        // Answer with results...
    }
}
```

## Authorization

### Defining Policies

```csharp
routing.ConfigureAuthorization(auth =>
{
    auth.AddPolicy("admin", p => p.RequirePermission("admin"));
    auth.AddPolicy("private-only", p => p.RequireChatType("Private"));
    auth.AddPolicy("moderator", p => p.RequireRank("moderator", "admin"));
    auth.AddPolicy("custom", p => p.AddRequirement(new MyCustomRequirement()));
});
```

### Protecting Handlers

```csharp
[TelegramCommand("/admin")]
[Authorize("admin")]
public class AdminHandler : ICommandHandler { ... }
```

Multiple `[Authorize]` attributes require ALL policies to pass.

### Custom Requirements

```csharp
public class VipRequirement : IAuthorizationRequirement
{
    public async Task<bool> IsSatisfiedAsync(TelegramUpdateContext context, CancellationToken ct)
    {
        // Check if user is VIP...
        return true;
    }
}
```

## Rate Limiting

```csharp
[TelegramCommand("/status")]
[RateLimit(10)]  // Max once per 10 seconds per user
public class StatusHandler : ICommandHandler { ... }
```

## Middleware

### Routing Middleware

Custom middleware in the update processing pipeline:

```csharp
public class MyMiddleware : ITelegramMiddleware
{
    public async Task InvokeAsync(TelegramUpdateContext context, TelegramUpdateDelegate next)
    {
        // Before handler
        await next(context);
        // After handler
    }
}

routing.UseMiddleware<MyMiddleware>();
```

### Built-in Middleware

| Middleware | Purpose |
|-----------|---------|
| `LoggingMiddleware` | Stopwatch + debug logging per update |
| `MetricsMiddleware` | Meter-based counters and latency for routed updates |
| `ExceptionHandlerMiddleware` | Catches unhandled exceptions |
| `LocalizationMiddleware` | Resolves user locale |
| `ConversationMiddleware` | Routes active conversations |
| `RateLimitMiddleware` | Per-user rate limiting |
| `AuthorizationMiddleware` | Policy-based authorization |

See [Metrics and Observability](metrics.md) for update throughput, latency, and routing rate-limit metrics.
