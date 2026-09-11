# Observability Stack

This folder provides a ready-to-run Prometheus + Grafana setup for TeleForge metrics.

## What is included

- `docker-compose.yml` for Prometheus and Grafana
- Prometheus scrape configuration in `prometheus/prometheus.yml`
- PrometheusRule examples in `prometheus/prometheus-rule-examples.yaml`
- Grafana datasource and dashboard provisioning in `grafana/provisioning/`
- Prebuilt dashboards in `grafana/dashboards/`

## Start stack

```bash
docker compose -f infrastructure/docker-compose.yml up -d
```

## Endpoints

- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin/admin)

## Configure your bot

Enable metrics in your app:

```csharp
builder.Services.AddTelegramObservability(options =>
{
    options.EnablePrometheusExporter = true;
    options.PrometheusUriPrefixes = ["http://localhost:9464/"];
});
```

Prometheus will scrape this endpoint from Docker using `host.docker.internal:9464`.

## Dashboards

- `telegram-overview.json`
- `messaging-pipeline.json`
- `routing-consumer.json`

Dashboards are provisioned automatically at Grafana startup.

## Alert Rules

`prometheus/prometheus-rule-examples.yaml` provides Kubernetes `PrometheusRule` examples for:

- high messaging retry rate
- circuit breaker rejection spikes
- routing failure ratio above threshold
