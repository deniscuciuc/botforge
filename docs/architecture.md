# Architecture

## Overview

BotForge is a modular framework built on the **Dependency Inversion Principle** — all contracts live in `*Abstractions` packages, while implementations are in separate packages. This allows consuming code to reference only the abstractions and swap implementations (e.g., in-memory → Redis rate limiting, channel → MassTransit queuing).

## Layer Diagram

```
┌───────────────────────────────────────────────────────────┐
│                      Consumer Layer                        │
│                                                            │
│   ┌─────────────┐    ┌─────────────────────┐              │
│   │ Long Polling │    │  Webhook Handler     │              │
│   │   Consumer   │    │  (ASP.NET Core)      │              │
│   └──────┬───────┘    └──────────┬──────────┘              │
│          │                       │                          │
│          ▼                       ▼                          │
│   ┌─────────────────────────────────────────┐              │
│   │           Update Channel                 │              │
│   │    (System.Threading.Channels)           │              │
│   └──────────────────┬──────────────────────┘              │
│                      │                                      │
│   ┌──────────────────▼──────────────────────┐              │
│   │        Update Worker (N threads)         │              │
│   └──────────────────┬──────────────────────┘              │
└──────────────────────┼──────────────────────────────────────┘
                       │
                       ▼
┌───────────────────────────────────────────────────────────┐
│                     Routing Layer                           │
│                                                            │
│   ┌─────────────────────────────────────────────────┐      │
│   │              Middleware Pipeline                  │      │
│   │   Logging → Authorization → Rate Limit →         │      │
│   │   Localization → Conversation → Exception        │      │
│   └──────────────────┬──────────────────────────────┘      │
│                      │                                      │
│   ┌──────────────────▼──────────────────────┐              │
│   │          Handler Dispatch                │              │
│   │   Route matching → Attribute discovery   │              │
│   │   → Handler resolution → Execution       │              │
│   └─────────────────────────────────────────┘              │
│                                                            │
│   Handlers: Command, Callback, Text, Media, Dice,          │
│   Inline, Contact, Location, ChatMember, PollAnswer        │
└───────────────────────────────────────────────────────────┘
                       │
                       ▼
┌───────────────────────────────────────────────────────────┐
│                    Messaging Layer                          │
│                                                            │
│   ┌─────────────────────────────────────────┐              │
│   │       Message Builder (Fluent API)       │              │
│   └──────────────────┬──────────────────────┘              │
│                      │                                      │
│   ┌──────────────────▼──────────────────────┐              │
│   │    Queue Backend                         │              │
│   │    (Channel / MassTransit)               │              │
│   └──────────────────┬──────────────────────┘              │
│                      │                                      │
│   ┌──────────────────▼──────────────────────┐              │
│   │       Send Pipeline (Middleware)         │              │
│   │   RateLimit → Retry → CircuitBreaker     │              │
│   │   → Metrics → Transport                  │              │
│   └──────────────────┬──────────────────────┘              │
│                      │                                      │
│   ┌──────────────────▼──────────────────────┐              │
│   │      Telegram API Transport              │              │
│   │      (Telegram.Bot SDK)                  │              │
│   └─────────────────────────────────────────┘              │
└───────────────────────────────────────────────────────────┘
```

## Package Dependency Graph

```
BotForge.Core (foundation)
│
├── BotForge.Routing.Abstractions
│   └── BotForge.Routing (handler discovery, middleware, authorization)
│
├── BotForge.Messaging.Abstractions
│   ├── BotForge.Messaging (send pipeline, builder, transport)
│   └── BotForge.Messaging.MassTransit (distributed queue)
│
├── BotForge.RateLimiting.Abstractions
│   └── BotForge.RateLimiting.Redis (Redis sliding window)
│
├── BotForge.Payments.Abstractions
│   ├── BotForge.Payments (checkout, invoices, refunds, payouts)
│   └── BotForge.Payments.Fragment (TON payout provider)
│
├── BotForge.Templates (YAML engine, keyboard builder, localization)
│
└── BotForge.Consumer (polling, webhook, hosted service)
```

## Key Design Decisions

### 1. Abstractions-First

Every major subsystem has a separate `*.Abstractions` package. This enables:
- Testing with mocks/fakes without loading implementations
- Swapping implementations without changing handler code
- Minimal dependency footprint for library consumers

### 2. Pipeline Architecture

Both the routing (update processing) and messaging (send) layers use a middleware pipeline pattern:
- **Routing pipeline**: `ITelegramMiddleware` with `InvokeAsync(context, next)`
- **Send pipeline**: `ISendMiddleware` with `InvokeAsync(context, next)`

Middleware is ordered — first registered executes first (wrapping subsequent middleware).

### 3. Attribute-Based Discovery

Handlers are discovered via reflection by scanning assemblies for types implementing handler interfaces and decorated with routing attributes:

```csharp
routing.AddHandlersFromAssembly(typeof(Program).Assembly);
```

### 4. Context Objects

Every handler receives a strongly-typed context with:
- `UpdateContext` — raw Telegram update, bot key, service provider
- `ChatId`, `UserId` — nullable convenience properties
- `BotId` — which bot received the update
- `RequestServices` — scoped DI container
- `CancellationToken` — cancellation support

### 5. Central Package Management

All NuGet versions are controlled via `Directory.Packages.props` at the repo root. Projects reference packages without specifying versions.

### 6. Single Version Source

The `VERSION` file at repo root is the single source of truth. Build scripts read it and inject it into `Directory.Build.props` for consistent NuGet package versioning.
