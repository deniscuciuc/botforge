# Services Architecture

The framework provides two deployable services — **Ingress** and **Sender** — that can run independently (split mode) or together in a single process (monolith mode).

## Deployment Modes

### Monolith Mode (Default)

Both services run in one process using an in-memory queue. Zero external dependencies beyond Telegram API.

```
┌──────────────────────────────────────────────┐
│                  Monolith                     │
│  Telegram ─► Consumer ─► Pipeline ─► Handler │
│                              │                │
│          in-memory queue     │                │
│                              ▼                │
│                        Send Pipeline ─► API   │
└──────────────────────────────────────────────┘
```

```bash
# Run via docker-compose
docker compose -f infrastructure/docker-compose.yml \
               -f infrastructure/docker-compose.monolith.yml \
               up ingress
```

### Split Mode

Ingress and Sender run as separate containers connected by RabbitMQ. Enables independent scaling.

```
┌─────────────────────┐     ┌──────────┐     ┌───────────────────────┐
│       Ingress       │     │ RabbitMQ │     │        Sender         │
│ Telegram ─► Consumer│ ──► │  Queue   │ ──► │ Send Pipeline ─► API  │
│         ─► Pipeline │     │          │     │                       │
└─────────────────────┘     └──────────┘     └───────────────────────┘
```

```bash
docker compose -f infrastructure/docker-compose.yml up
```

## Service Details

### Ingress Service

Receives Telegram updates via long polling or webhook, processes them through the routing pipeline (middleware + handlers), and optionally enqueues outbound messages.

**Port:** 8080 (HTTP) / 9464 (Prometheus metrics)

**Key endpoints:**
- `POST /bot/{botId}/webhook` — Webhook receiver
- `GET /healthz` — Liveness probe
- `GET /readyz` — Readiness probe

### Sender Service

Consumes messages from the queue and delivers them to the Telegram Bot API through the send pipeline (rate limiting, retry, circuit breaker).

**Port:** 8081 (HTTP) / 9465 (Prometheus metrics)

**Key endpoints:**
- `GET /healthz` — Liveness probe
- `GET /readyz` — Readiness probe

## Configuration

All configuration follows `Telegram:*` section binding. Override via environment variables or appsettings.

### Bots

```json
{
  "Telegram": {
    "Bots": [
      {
        "Key": "main",
        "Token": "123456:ABC-DEF",
        "Transport": "LongPolling",
        "AllowedUpdates": ["message", "callback_query"],
        "GlobalPerSecond": 30,
        "PerChatPerSecond": 1,
        "GroupPerMinute": 20
      }
    ]
  }
}
```

### Queue

| Key | Default | Description |
|-----|---------|-------------|
| `Queue:Backend` | `InMemory` | `InMemory` or `RabbitMq` |
| `Queue:RabbitMqConnectionString` | — | AMQP connection string |

### Consumer (Ingress only)

| Key | Default | Description |
|-----|---------|-------------|
| `Consumer:ConcurrencyLimit` | `10` | Max concurrent update processing |
| `Consumer:ChannelCapacity` | `1000` | Internal channel buffer size |
| `Consumer:PollingLimit` | `100` | Max updates per polling request |
| `Consumer:GracefulShutdownSeconds` | `30` | Shutdown drain timeout |

### Send (Sender/Monolith)

| Key | Default | Description |
|-----|---------|-------------|
| `Send:RetryCount` | `3` | Max retry attempts |
| `Send:RetryBaseDelaySeconds` | `1` | Base delay for exponential backoff |
| `Send:MaxRetryDelaySeconds` | `60` | Max delay cap |
| `Send:CircuitBreakerFailureThreshold` | `5` | Failures before breaking |
| `Send:CircuitBreakerBreakDurationSeconds` | `30` | Circuit breaker open duration |

### Observability

| Key | Default | Description |
|-----|---------|-------------|
| `Observability:EnableMetrics` | `true` | Enable/disable metrics |
| `Observability:EnablePrometheusExporter` | `true` | Enable Prometheus HTTP endpoint |
| `Observability:PrometheusPort` | `9464` | Prometheus scrape port |

## Multi-Bot Support

Add multiple entries to the `Bots` array. Each bot is independently configured with its own token, transport, rate limits, and allowed updates. The framework routes updates to the correct pipeline based on the bot key.

## Extensibility

### Custom Handlers

Register handlers from external assemblies:

```csharp
services.AddTelegramIngressHost(configuration, options =>
{
    options.HandlerAssemblies.Add("MyApp.Handlers");
});
```

### Custom Queue Backend

Implement `IMessageQueueBackend` and configure via messaging options:

```csharp
services.AddTelegramMessaging(m => m.UseQueueBackend<MyCustomBackend>());
```

### Custom Send Middleware

Implement `ISendMiddleware` and register it in the pipeline:

```csharp
services.AddTelegramMessaging(m => m.UseSendMiddleware<MyMiddleware>());
```

## Scaling Recommendations

| Component | Scaling Strategy |
|-----------|-----------------|
| Ingress | Horizontal (stateless) — multiple replicas behind load balancer |
| Sender | Horizontal — add replicas to increase throughput; RabbitMQ distributes messages |
| RabbitMQ | Cluster with quorum queues for HA |
| Redis | Sentinel or Cluster for rate limit store HA |
