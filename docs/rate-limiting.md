# Rate Limiting

## Overview

Rate limiting operates at two levels:

1. **Handler-level** — Per-user cooldowns on commands/callbacks via `[RateLimit]` attribute
2. **Transport-level** — Bot-wide and per-chat limits in the send pipeline via `RateLimitSendMiddleware`

## Handler Rate Limiting

### Using the Attribute

```csharp
[TelegramCommand("/status")]
[RateLimit(10)]  // Max once per 10 seconds per user
public class StatusHandler : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        // ...
        return CommandResult.Ok();
    }
}
```

The `[RateLimit(seconds)]` attribute is checked by `RateLimitMiddleware` in the routing pipeline. If the user exceeds the limit, the handler is not invoked.

## Transport Rate Limiting

### Send Pipeline Middleware

`RateLimitSendMiddleware` runs in the send pipeline and enforces:

- **Global bot limit** — `BotRateLimitOptions.GlobalPerSecond` (default: 30/s)
- **Per-chat limit** — `BotRateLimitOptions.PerChatPerSecond` (default: 1/s)

```csharp
messaging.AddBot("main", bot =>
{
    bot.Token = "BOT_TOKEN";
    bot.RateLimit.GlobalPerSecond = 30;
    bot.RateLimit.PerChatPerSecond = 1;
    bot.RateLimit.GroupPerMinute = 20;
});
```

When throttled, the middleware **waits** for `WaitTime` then proceeds (does not reject).

### Bypassing Rate Limits

```csharp
await messaging.CreateMessage("main")
    .ToChat(chatId)
    .WithText("Urgent alert!")
    .BypassRateLimit()
    .SendAsync(ct);
```

## Rate Limit Stores

### IRateLimitStore Interface

```csharp
public interface IRateLimitStore
{
    Task<RateLimitResult> AcquireAsync(string key, RateLimitPolicy policy, CancellationToken ct);
    Task ResetAsync(string key, CancellationToken ct);
}
```

### RateLimitPolicy

```csharp
public class RateLimitPolicy
{
    public string Key { get; init; }
    public int PermitsPerWindow { get; init; }
    public TimeSpan Window { get; init; }
}
```

### RateLimitResult

| Property | Type | Description |
|----------|------|-------------|
| `IsAllowed` | `bool` | Whether the request passed |
| `WaitTime` | `TimeSpan?` | How long to wait if throttled |
| `RemainingPermits` | `int` | Remaining permits in window |

Factory methods: `RateLimitResult.Allowed(remaining)`, `RateLimitResult.Throttled(waitTime)`

## In-Memory Store (Default)

Registered automatically with `AddTelegramMessaging`. Uses `System.Threading.RateLimiting.TokenBucketRateLimiter` per key.

- Token bucket with `PermitsPerWindow` tokens per `Window`
- Auto-replenishment enabled
- Suitable for single-process deployments
- Implements `IDisposable` — disposes all limiters on shutdown

## Redis Store

For distributed deployments where multiple processes share rate limit state:

```csharp
builder.Services.AddRedisRateLimitStore(keyPrefix: "tg:ratelimit:");
```

### How It Works

- Uses a **sliding window counter** implemented as a Redis sorted set
- Entries scored by timestamp; expired entries pruned via `ZREMRANGEBYSCORE`
- Executes as an atomic Lua script for consistency
- **Fail-open**: on Redis errors, requests are allowed (with a warning log)

### Prerequisites

Requires `IConnectionMultiplexer` registered in DI:

```csharp
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379"));

builder.Services.AddRedisRateLimitStore();
```

### Custom Key Prefix

The default key prefix is `tg:ratelimit:`. Override it to namespace your keys:

```csharp
builder.Services.AddRedisRateLimitStore("mybot:limits:");
// Keys: mybot:limits:bot:main:global, mybot:limits:bot:main:chat:12345
```

## Custom Store

```csharp
public class MyRateLimitStore : IRateLimitStore
{
    public async Task<RateLimitResult> AcquireAsync(
        string key, RateLimitPolicy policy, CancellationToken ct)
    {
        // Your implementation
    }

    public async Task ResetAsync(string key, CancellationToken ct)
    {
        // Delete the key
    }
}

// Register
messaging.UseRateLimitStore<MyRateLimitStore>();
```

See [Metrics and Observability](metrics.md) for limiter throughput, throttle, and retry-after telemetry.
