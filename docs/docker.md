# Docker Guide

## Quick Start

### Split Mode (Ingress + Sender + RabbitMQ)

```bash
cd infrastructure
docker compose up -d
```

Services:
- **Ingress:** http://localhost:8080
- **Sender:** http://localhost:8081
- **RabbitMQ Management:** http://localhost:15672 (guest/guest)
- **Prometheus:** http://localhost:9090
- **Grafana:** http://localhost:3000

### Monolith Mode (Single process, no RabbitMQ)

```bash
cd infrastructure
docker compose -f docker-compose.yml -f docker-compose.monolith.yml up -d ingress redis prometheus grafana
```

## Building Images Locally

```bash
# From repository root
./scripts/docker-build.sh

# Or manually:
docker build -t telegram-ingress:local -f services/BotForge.Ingress/Dockerfile .
docker build -t telegram-sender:local -f services/BotForge.Sender/Dockerfile .
```

## Environment Variables

Configuration follows .NET's `__` separator convention for nested keys.

### Common

| Variable | Description |
|----------|-------------|
| `Telegram__Bots__0__Key` | Bot identifier key |
| `Telegram__Bots__0__Token` | Telegram Bot API token |
| `Telegram__Queue__Backend` | `InMemory` or `RabbitMq` |
| `Telegram__Queue__RabbitMqConnectionString` | AMQP connection string |
| `Telegram__Observability__EnableMetrics` | `true`/`false` |
| `Telegram__Observability__PrometheusPort` | Metrics port |

### Ingress-specific

| Variable | Description |
|----------|-------------|
| `Telegram__Bots__0__Transport` | `LongPolling` or `Webhook` |
| `Telegram__Consumer__ConcurrencyLimit` | Max concurrent update processing |
| `Telegram__Webhook__Path` | Webhook URL path |
| `Telegram__Webhook__SecretToken` | Webhook secret for validation |

## Health Checks

Both services expose:
- `GET /healthz` — Liveness (always 200 when process is running)
- `GET /readyz` — Readiness (200 when dependencies are connected)

Docker containers include built-in `HEALTHCHECK` directives using `/healthz`.

## Building images

No prebuilt images are published — the two hosts are thin shells around
`BotForge.Hosting`, and most deployments want their own handlers compiled in anyway. Build
them from the Dockerfiles in `services/`:

```bash
docker build -f services/BotForge.Ingress/Dockerfile -t your-registry/botforge-ingress:1.0.0 .
docker build -f services/BotForge.Sender/Dockerfile  -t your-registry/botforge-sender:1.0.0 .
```

## Production Checklist

- [ ] Set real bot tokens via environment variables or secrets
- [ ] Use a proper RabbitMQ cluster (not guest/guest)
- [ ] Configure Redis password and TLS
- [ ] Set resource limits (CPU/memory) on containers
- [ ] Mount persistent volumes for RabbitMQ and Redis data
- [ ] Configure Grafana dashboards and alerting rules
- [ ] Review rate limit settings per bot
- [ ] Enable webhook with HTTPS and secret token validation
- [ ] Set `GracefulShutdownSeconds` appropriate for your workload
