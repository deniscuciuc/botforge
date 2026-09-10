# AdvancedBot

A comprehensive example demonstrating advanced framework features: **multi-bot**, **Redis rate limiting**, **MassTransit
message queuing**, **authorization policies**, and **custom send middleware**.

## Features

- **Multi-Bot Setup** — Two bots (`main` + `notifications`) with independent tokens
- **Redis Rate Limiting** — Distributed rate limiting via `StackExchange.Redis`
- **MassTransit + RabbitMQ** — Message queue backend for reliable delivery
- **Authorization Policies** — Role-based access control with `[Authorize]`
- **Custom Send Middleware** — Audit logging for outgoing messages
- **Per-Command Rate Limiting** — `[RateLimit(seconds)]` attribute
- **Health Checks** — Consumer health monitoring
- **Send Pipeline** — Full middleware chain: rate limit → retry → circuit breaker → metrics
- **Prometheus Metrics Export** — Optional exporter via `AddTelegramObservability()`

## Bot Commands

| Command     | Description               | Policy         |
|-------------|---------------------------|----------------|
| `/start`    | Welcome message           | —              |
| `/notify`   | Send via notification bot | —              |
| `/admin`    | Admin-only command        | `admin`        |
| `/moderate` | Moderator command         | `moderator`    |
| `/private`  | Private chat only         | `private-only` |
| `/status`   | Bot status (rate limited) | —              |

## How It Works

### Multi-Bot Architecture

Two bots are registered with separate tokens:

```csharp
messaging.AddBot("main", bot => { bot.Token = "..."; });
messaging.AddBot("notifications", bot => { bot.Token = "..."; });
```

The `/notify` command receives the update on the `main` bot, then sends a notification via
`messaging.CreateMessage("notifications")`.

### Redis Rate Limiting

Replaces the default in-memory rate limit store with Redis for distributed deployments:

```csharp
services.AddRedisRateLimitStore("advbot:ratelimit:");
```

Uses a Lua sliding-window algorithm for accurate distributed rate limiting.

### MassTransit Message Queue

Messages queued with `.QueueAsync()` are published to RabbitMQ via MassTransit:

```csharp
services.AddMassTransit(bus =>
{
    bus.AddTelegramMessageConsumer();
    bus.UsingRabbitMq((ctx, cfg) => { ... });
});
services.AddTelegramMassTransitBackend();
```

`TelegramMessageConsumer` picks messages from the bus and sends them via the `ISendPipeline`.

### Authorization Policies

Three policies defined in routing configuration:

```csharp
routing.ConfigureAuthorization(auth =>
{
    auth.AddPolicy("admin", p => p.RequirePermission("admin"));
    auth.AddPolicy("private-only", p => p.RequireChatType("Private"));
    auth.AddPolicy("moderator", p => p.RequireRank("moderator", "admin"));
});
```

Handlers use `[Authorize("policyName")]` to require authorization.

### Send Pipeline

The send pipeline wraps every outgoing message:

```text
RateLimitSendMiddleware → RetrySendMiddleware → CircuitBreakerSendMiddleware → MetricsSendMiddleware → Transport
```

Custom middleware (like `AuditSendMiddleware`) can be added with `UseSendMiddleware<T>()`.

## Project Structure

```text
AdvancedBot/
├── Program.cs                          # Full DI setup with all integrations
├── AdvancedBot.csproj                  # References MassTransit, Redis, Messaging, Routing
├── appsettings.json                    # Bot tokens, Redis, RabbitMQ config
├── Handlers/
│   └── CommandHandlers.cs             # All command handlers with [Authorize], [RateLimit]
├── Middleware/
│   └── AuditSendMiddleware.cs         # Custom ISendMiddleware example
└── README.md
```

## Prerequisites

- **Redis** — Required for distributed rate limiting
- **RabbitMQ** — Required for MassTransit message queue

### Quick Start with Docker

```bash
docker run -d --name redis -p 6379:6379 redis:7
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

## How to Run

1. Start Redis and RabbitMQ (see above)
2. Create two bots via [@BotFather](https://t.me/BotFather) — one main, one for notifications
3. Configure tokens in `appsettings.json`:

    ```json
    {
      "Telegram": {
        "MainBotToken": "123456:ABC...",
        "NotificationBotToken": "789012:DEF..."
      }
    }
    ```

4. Run the project:

    ```bash
    dotnet run --project examples/AdvancedBot
    ```

## Observability

AdvancedBot enables framework metrics export through:

```csharp
builder.Services.AddTelegramObservability(options =>
{
  options.EnablePrometheusExporter = true;
  options.PrometheusUriPrefixes = ["http://localhost:9464/"];
});
```

To run Grafana + Prometheus locally:

```bash
docker compose -f infrastructure/docker-compose.yml up -d
```

Then open:

- Grafana: [http://localhost:3000](http://localhost:3000)
- Prometheus: [http://localhost:9090](http://localhost:9090)

## Key Framework Features Demonstrated

- `TelegramMessagingOptions.AddBot()` — Multi-bot registration
- `AddRedisRateLimitStore()` — Redis-backed distributed rate limiting
- `AddTelegramMassTransitBackend()` — MassTransit message queue backend
- `IBusRegistrationConfigurator.AddTelegramMessageConsumer()` — MassTransit consumer
- `[Authorize("policy")]` — Authorization attribute
- `AuthorizationConfiguration.AddPolicy()` — Policy registration
- `PolicyBuilder.RequirePermission()` / `RequireRank()` / `RequireChatType()` — Policy requirements
- `[RateLimit(seconds)]` — Per-handler rate limiting
- `ISendMiddleware` — Custom send pipeline middleware
- `TelegramConsumerOptions.EnableHealthChecks()` — Health check registration
- `ITelegramMessage.QueueAsync()` — Queue messages via MassTransit
