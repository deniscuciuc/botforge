# Metrics and Observability

## Overview

BotForge now emits framework metrics through `System.Diagnostics.Metrics` across messaging, routing, consumer, templates, and payments.

Metrics export is optional and disabled by default. To expose metrics for Prometheus, enable observability explicitly:

```csharp
using BotForge.Observability;

builder.Services.AddTelegramObservability(options =>
{
    options.EnablePrometheusExporter = true;
    options.PrometheusUriPrefixes = ["http://localhost:9464/"];
});
```

Each feature library owns its own meter and metric definitions:

- Messaging metrics live in `BotForge.Messaging`
- Routing metrics live in `BotForge.Routing`
- Consumer metrics live in `BotForge.Consumer`
- Template metrics live in `BotForge.Templates`
- Payment metrics live in `BotForge.Payments`

`AddTelegramObservability()` configures optional runtime toggles per feature and exporter wiring. Example:

```csharp
builder.Services.AddTelegramObservability(options =>
{
    options.EnableMetrics = true;
    options.EnableMessagingMetrics = true;
    options.EnableRoutingMetrics = true;
    options.EnableConsumerMetrics = false;
    options.EnableTemplateMetrics = true;
    options.EnablePaymentMetrics = true;
    options.EnablePrometheusExporter = true;
});
```

## Cardinality Rules

Use only bounded labels for production-safe metrics:

- Allowed: `bot_key`, `update_type`, `operation`, `status`, `scope`, `state`, `currency`, `provider`, `payload_prefix`
- Avoid: `chat_id`, `user_id`, callback payload content, free-form text

## Meter Names

- `BotForge.Messaging`
- `BotForge.Routing`
- `BotForge.Consumer`
- `BotForge.Templates`
- `BotForge.Payments`

## Metrics Catalog

### Messaging

| Metric | Type | Unit | Labels | Meaning |
|---|---|---|---|---|
| `botforge.telegram.messaging.send.requests` | Counter | request | `bot_key`, `operation`, `status`, `error_code_class` | Total send requests processed by send pipeline |
| `botforge.telegram.messaging.send.duration` | Histogram | ms | `bot_key`, `operation`, `status`, `error_code_class` | End-to-end send duration |
| `botforge.telegram.messaging.ratelimit.acquire` | Counter | attempt | `bot_key`, `scope`, `outcome` | Messaging limiter checks and outcomes |
| `botforge.telegram.messaging.ratelimit.wait.duration` | Histogram | ms | `bot_key`, `scope` | Delay added by rate limiting |
| `botforge.telegram.messaging.retry.attempts` | Counter | attempt | `bot_key` | Retry attempts performed |
| `botforge.telegram.messaging.circuitbreaker.transitions` | Counter | transition | `bot_key`, `state` | Circuit breaker state transitions |
| `botforge.telegram.messaging.circuitbreaker.rejections` | Counter | request | `bot_key` | Requests rejected while circuit is open |

### Routing

| Metric | Type | Unit | Labels | Meaning |
|---|---|---|---|---|
| `botforge.telegram.routing.updates.total` | Counter | update | `bot_key`, `update_type`, `status` | Total updates processed by routing pipeline |
| `botforge.telegram.routing.updates.duration` | Histogram | ms | `bot_key`, `update_type`, `status` | Routing pipeline duration |
| `botforge.telegram.routing.ratelimit.checks` | Counter | check | `update_kind`, `outcome` | Routing rate-limit checks and denials |
| `botforge.telegram.routing.handler.calls` | Counter | call | `bot_key`, `query_type`, `query`, `status` | Per-handler execution count by query type |
| `botforge.telegram.routing.handler.latency` | Histogram | ms | `bot_key`, `query_type`, `query`, `status` | Per-handler execution duration by query type |
| `botforge.telegram.routing.ratelimit.retry_after` | Histogram | ms | `update_kind` | Retry-after durations returned by routing limiter |


**Bot Query Latency — label cardinality rules:**

- `query` label values are bounded to: command names (`/start`, `/status`, `/search`, ...),
  callback-query patterns (`product:{id}`, `page:{n}`, ...), handler type names, or media type names.
- Raw callback data, user-supplied text, chat IDs, and user IDs are **never** included as label values.
- `query_type` is one of: `command`, `callback_query`, `text_message`, `inline_query`,
  `chosen_inline_result`, `media`, `location`, `contact`, `users_shared`, `chat_shared`,
  `poll_answer`, `chat_member`, `dice`, `gift_message`, `unique_gift_message`.
- `status` is one of: `success`, `blocked`, `exception`.

### Consumer

| Metric | Type | Unit | Labels | Meaning |
|---|---|---|---|---|
| `botforge.telegram.consumer.updates.ingested` | Counter | update | `bot_key`, `transport`, `update_type` | Ingested updates from polling/webhook |
| `botforge.telegram.consumer.updates.processed` | Counter | update | `bot_key`, `update_type`, `status` | Processed updates by workers |
| `botforge.telegram.consumer.updates.processing.duration` | Histogram | ms | `bot_key`, `update_type`, `status` | Worker processing duration |
| `botforge.telegram.consumer.updates.queue_lag` | Histogram | ms | `bot_key`, `update_type` | Time spent queued before worker processing |
| `botforge.telegram.consumer.workers.active` | UpDownCounter | worker | none | Number of active worker loops |

### Templates

| Metric | Type | Unit | Labels | Meaning |
|---|---|---|---|---|
| `botforge.telegram.templates.render.total` | Counter | render | `template`, `language`, `status` | Template render calls |
| `botforge.telegram.templates.render.duration` | Histogram | ms | `template`, `language`, `status` | Template render duration |
| `botforge.telegram.templates.keyboard.build.total` | Counter | build | `template`, `dynamic`, `status` | Keyboard build calls |
| `botforge.telegram.templates.keyboard.build.duration` | Histogram | ms | `template`, `dynamic`, `status` | Keyboard build duration |

### Payments

| Metric | Type | Unit | Labels | Meaning |
|---|---|---|---|---|
| `botforge.telegram.payments.invoice.created` | Counter | invoice | `bot_key`, `invoice_type`, `currency` | Invoices created |
| `botforge.telegram.payments.checkout.completed` | Counter | checkout | `bot_key`, `payload_prefix`, `approved` | Pre-checkout decisions |
| `botforge.telegram.payments.payment.outcome` | Counter | payment | `bot_key`, `payload_prefix`, `status`, `currency` | Payment outcomes |
| `botforge.telegram.payments.payment.amount` | Histogram | star | `bot_key`, `payload_prefix`, `status`, `currency` | Payment amount in stars |
| `botforge.telegram.payments.refund.processed` | Counter | refund | `bot_key`, `reason` | Refund operations |
| `botforge.telegram.payments.refund.amount` | Histogram | star | `bot_key`, `reason` | Refund amount in stars |
| `botforge.telegram.payments.payout.requested` | Counter | payout | `provider`, `currency` | Payout requests |
| `botforge.telegram.payments.payout.completed` | Counter | payout | `provider`, `currency`, `status` | Payout outcomes |
| `botforge.telegram.payments.payout.duration` | Histogram | ms | `provider` | Payout duration |
| `botforge.telegram.payments.gift.sent` | Counter | gift | `bot_key`, `gift_type` | Gift sends |
| `botforge.telegram.payments.paid_media.sold` | Counter | sale | `bot_key` | Paid media sales |
| `botforge.telegram.payments.paid_media.stars` | Histogram | star | `bot_key` | Stars sold per paid media sale |
| `botforge.telegram.payments.antifraud.check` | Counter | check | `check`, `passed` | Anti-fraud check outcomes |
| `botforge.telegram.payments.processing.duration` | Histogram | ms | `bot_key`, `payload_prefix` | Payment processing duration |

Notes for payment labels:

- `reason` and `check` labels are normalized to lowercase ASCII tokens.
- Distinct values are capped (16 per process for each label family).
- Additional unseen values are folded into `other` to prevent high-cardinality blowups.

## Prometheus and Grafana

Repository assets are available in `infrastructure/`:

- `infrastructure/docker-compose.yml`
- `infrastructure/prometheus/prometheus.yml`
- `infrastructure/prometheus/prometheus-rule-examples.yaml`
- `infrastructure/grafana/dashboards/*.json`

Start local stack:

```bash
docker compose -f infrastructure/docker-compose.yml up -d
```

Then open:

- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000` (`admin` / `admin`)

## Dashboard Pack

Included dashboards:

- `BotForge Telegram Overview`
- `BotForge Messaging Pipeline`
- `BotForge Routing and Consumer`

These are provisioned automatically by Grafana from `infrastructure/grafana/provisioning/`.

### Bot Query Latency panels

The routing dashboard should include the following panels for the new handler-level metrics:

1. **Slowest commands (p95)** — `histogram_quantile(0.95, sum(rate(botforge_telegram_routing_handler_latency_bucket{query_type="command"}[5m])) by (le, query))`
2. **Handler calls by type** — `sum(rate(botforge_telegram_routing_handler_calls_total[5m])) by (query_type)`
3. **Command success vs error rate** — `rate(botforge_telegram_routing_handler_calls_total{query_type="command"}[5m])` with `by (query, status)`
4. **Callback query latency heatmap** — `histogram_quantile(0.99, sum(rate(botforge_telegram_routing_handler_latency_bucket{query_type="callback_query"}[5m])) by (le, query))`


## PrometheusRule Examples

For Kubernetes Prometheus Operator users, see `infrastructure/prometheus/prometheus-rule-examples.yaml`.

Included alert examples:

- high retry rate in messaging pipeline
- circuit breaker rejection spikes
- routing failure ratio over total updates
 - slow command/p95 handler latency exceeding 5 s for any command in a 5m window
