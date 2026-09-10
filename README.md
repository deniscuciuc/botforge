# BotForge

[![CI](https://github.com/deniscuciuc/botforge/actions/workflows/ci.yml/badge.svg)](https://github.com/deniscuciuc/botforge/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/BotForge.Core.svg?label=BotForge.Core)](https://www.nuget.org/packages/BotForge.Core/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> A Telegram bot framework for .NET. Attribute routing, a middleware pipeline, YAML message
> templates, and a payments stack that handles Telegram Stars, TON and Fragment.

`Telegram.Bot` gives you an API client. BotForge gives you the application around it: updates
arrive on one pipeline, replies leave on another, and both are yours to extend.

```
dotnet add package BotForge.Hosting
```

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTelegramMessaging(messaging =>
    messaging.AddBot("main", bot => bot.Token = builder.Configuration["Telegram:BotToken"]!));

builder.Services.AddTelegramRouting(routing =>
    routing.AddHandlersFromAssembly(typeof(Program).Assembly));

builder.Services.AddTelegramConsumer(consumer => consumer.ConcurrencyLimit = 10);

await builder.Build().RunAsync();
```

A handler is a class with an attribute:

```csharp
[TelegramCommand("/start")]
public class StartHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("Hi! Send me anything and I'll echo it back.")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}
```

That is a complete bot. Fifteen more, from an echo bot to a full storefront, are in
[`examples/`](examples/).

## How it fits together

```
long polling or webhook
        ↓
   update channel  ──────────────  bounded, N workers
        ↓
   routing pipeline               logging → auth → rate limit → localization
        ↓                         → conversation → exception → handler
   your handler
        ↓
   message builder
        ↓
   send queue  ───────────────────  in-memory, or MassTransit over RabbitMQ
        ↓
   send pipeline                  rate limit → retry → circuit breaker → metrics
        ↓
   Telegram API
```

Two independent pipelines with a queue between them. That queue is why the sender can be a
separate process: run one ingress and several senders, or a single monolith host — the
handler code is identical either way. See [docs/architecture.md](docs/architecture.md).

Multi-bot is first class. Everything is keyed by `botId`, so one process can serve many bots.

## What you get

| | |
|---|---|
| **Routing** | `[TelegramCommand]`, `[CallbackQuery]`, `[TextMessage]`, `[ChatType]`, `[Authorize]`, `[RateLimit]`, plus typed handlers for media, dice, gifts, polls, inline queries, contacts, locations and chat-member updates |
| **Middleware** | Both directions. `ITelegramMiddleware` on the way in, `ISendMiddleware` on the way out — the same shape as ASP.NET Core's, including `next` |
| **Templates** | YAML message templates with conditionals, loops, partials, formatters, an emoji registry and keyboard definitions, so copy changes without a redeploy |
| **Payments** | Invoices, pre-checkout validation, payment processing, refunds, subscriptions, Telegram Stars, gifts, paid media, an anti-fraud pipeline, and payouts via TON or Fragment |
| **Rate limiting** | Per-user, Redis-backed, so limits hold across every instance |
| **Streaming** | Draft-message streaming, for sending a reply progressively as it is generated |
| **Observability** | OpenTelemetry metrics with Grafana dashboards in [`infrastructure/`](infrastructure/) |
| **Analyzers** | `BotForge.Analyzers` catches what the compiler cannot: a handler with the wrong return type, a duplicate route, a handler class missing its attribute |
| **Testing** | `BotForge.TestUtilities` gives you a fake client that records what was sent, and an update factory. No assertion framework attached |

## Packages

Start with `BotForge.Hosting` — it pulls in routing, messaging and the consumer.

| Package | |
|---|---|
| `BotForge.Hosting` | Host wiring: monolith, or split ingress and sender |
| `BotForge.Core` | Update context, middleware contract, client provider |
| `BotForge.Routing`, `.Routing.Abstractions` | Attribute routing and the update pipeline |
| `BotForge.Messaging`, `.Messaging.Abstractions` | Message builder, send pipeline, in-memory queue |
| `BotForge.Messaging.MassTransit` | RabbitMQ queue backend |
| `BotForge.Consumer` | Long-polling ingress |
| `BotForge.Templates` | YAML message templates |
| `BotForge.Payments`, `.Payments.Abstractions` | The payments stack |
| `BotForge.Payments.Crypto`, `.Payments.Fragment` | TON and Fragment payouts |
| `BotForge.RateLimiting.Redis`, `.RateLimiting.Abstractions` | Distributed rate limiting |
| `BotForge.Streaming` | Draft-message streaming |
| `BotForge.Observability` | OpenTelemetry metrics |
| `BotForge.Contracts` | Shared contracts for a split deployment |
| `BotForge.Analyzers` | Roslyn analyzers and code fixes |
| `BotForge.TestUtilities` | Test doubles |
| `BotForge.Templates.DotNet` | `dotnet new botforge-bot` and `dotnet new botforge-handler` |

Reference an `.Abstractions` package on its own when a project defines handlers or contracts
but does not host them.

## Localization

`BotForge.Templates` resolves `{@key}` tokens through `ILocalizationKeyResolver`. Implement
it against whatever holds your translations — a database, resx, a spreadsheet — and register
it; the template engine picks it up. Nothing is bundled, so BotForge does not pull a
localization stack into your dependency graph.

## Status

**0.1.0.** The framework is used in production and the API is settled in shape, but this is
its first public release and it stays below 1.0 until the payments surface has been exercised
by someone other than its author. Breaking changes will be called out in
[CHANGELOG.md](CHANGELOG.md).

Two things worth knowing before you adopt it:

- `BotForge.Observability` depends on OpenTelemetry's Prometheus exporter, which has **never
  had a stable release** — every published version is `-beta`. If a prerelease in your
  dependency graph is a problem, skip that package and export metrics your own way; the
  meters are plain `System.Diagnostics.Metrics` instruments.
- `BotForge.Payments.Fragment` and `.Payments.Crypto` talk to third-party services
  (`api.fragment-api.com`, `toncenter.com`, `app.tonkeeper.com`). Read their terms before you
  put them in front of real money.

Targets **net10.0**. Building from source needs the **.NET 10 SDK**.

## Documentation

- [Architecture](docs/architecture.md)
- [Routing](docs/routing.md) · [Conversations](docs/conversations.md)
- [Messaging](docs/messaging.md) · [Rate limiting](docs/rate-limiting.md)
- [Templates](docs/templates.md)
- [Payments](docs/payments.md)
- [Metrics](docs/metrics.md) · [Services and deployment](docs/services.md) · [Docker](docs/docker.md)
- [Release process](docs/release-process.md)

## Contributing

Issues and pull requests are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md),
[CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) and [SECURITY.md](SECURITY.md).

## License

[MIT](LICENSE)
