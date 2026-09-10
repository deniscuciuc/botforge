namespace BotForge.Hosting;

/// <summary>
/// Queue backend selection for inter-service communication.
/// </summary>
public enum QueueBackend
{
    InMemory,
    RabbitMq
}

/// <summary>
/// Configuration for the Ingress service host.
/// Bound from <c>Telegram</c> configuration section.
/// </summary>
public sealed class IngressHostOptions
{
    public List<BotEntry> Bots { get; set; } = [];
    public ConsumerSettings Consumer { get; set; } = new();
    public WebhookSettings Webhook { get; set; } = new();
    public QueueSettings Queue { get; set; } = new();
    public ObservabilitySettings Observability { get; set; } = new();
    public List<string> HandlerAssemblies { get; set; } = [];
}

/// <summary>
/// Configuration for the Sender service host.
/// Bound from <c>Telegram</c> configuration section.
/// </summary>
public sealed class SenderHostOptions
{
    public List<BotEntry> Bots { get; set; } = [];
    public QueueSettings Queue { get; set; } = new();
    public SendSettings Send { get; set; } = new();
    public ObservabilitySettings Observability { get; set; } = new();
}

/// <summary>
/// Configuration for the Monolith host (Ingress + Sender in one process).
/// </summary>
public sealed class MonolithHostOptions
{
    public List<BotEntry> Bots { get; set; } = [];
    public ConsumerSettings Consumer { get; set; } = new();
    public WebhookSettings Webhook { get; set; } = new();
    public QueueSettings Queue { get; set; } = new();
    public SendSettings Send { get; set; } = new();
    public ObservabilitySettings Observability { get; set; } = new();
    public List<string> HandlerAssemblies { get; set; } = [];
}

public sealed class BotEntry
{
    public required string Key { get; set; }
    public required string Token { get; set; }
    public string Transport { get; set; } = "LongPolling";
    public string[] AllowedUpdates { get; set; } = [];
    public int GlobalPerSecond { get; set; } = 30;
    public int PerChatPerSecond { get; set; } = 1;
    public int GroupPerMinute { get; set; } = 20;
}

public sealed class ConsumerSettings
{
    public int ConcurrencyLimit { get; set; } = 50;
    public int ChannelCapacity { get; set; } = 1000;
    public int GracefulShutdownSeconds { get; set; } = 30;
    public int PollingLimit { get; set; } = 100;
}

public sealed class WebhookSettings
{
    public string Path { get; set; } = "/api/telegram/webhook/{botId}";
    public string? SecretToken { get; set; }
    public int MaxConnections { get; set; } = 40;
    public bool DropPendingUpdates { get; set; }
}

public sealed class QueueSettings
{
    public QueueBackend Backend { get; set; } = QueueBackend.InMemory;
    public string? RabbitMqConnectionString { get; set; }
    public string QueuePrefix { get; set; } = "telegram";
}

public sealed class SendSettings
{
    public int RetryCount { get; set; } = 3;
    public double RetryBaseDelaySeconds { get; set; } = 1;
    public double MaxRetryDelaySeconds { get; set; } = 60;
    public int CircuitBreakerFailureThreshold { get; set; } = 10;
    public double CircuitBreakerBreakDurationSeconds { get; set; } = 30;
    public bool UseRetryAfterForBreakDuration { get; set; } = true;
}

public sealed class ObservabilitySettings
{
    public bool EnableMetrics { get; set; } = true;
    public bool EnablePrometheusExporter { get; set; } = true;
    public int PrometheusPort { get; set; } = 9464;
}

public sealed class RedisSettings
{
    public string? ConnectionString { get; set; }
    public string KeyPrefix { get; set; } = "telegram:ratelimit:";
}
