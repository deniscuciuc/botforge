using TeleForge.Core;

namespace TeleForge.Consumer;

public class TelegramConsumerOptions
{
    public UpdateTransport DefaultTransport { get; set; } = UpdateTransport.LongPolling;
    public int ConcurrencyLimit { get; set; } = 50;
    public int ChannelCapacity { get; set; } = 1000;
    public TimeSpan GracefulShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int PollingTimeoutSeconds { get; set; } = 30;
    public int PollingLimit { get; set; } = 100;

    internal WebhookOptions Webhook { get; } = new();
    internal HealthCheckOptions HealthCheck { get; } = new();

    public TelegramConsumerOptions ConfigureWebhook(Action<WebhookOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(Webhook);
        return this;
    }

    public TelegramConsumerOptions EnableHealthChecks(Action<HealthCheckOptions>? configure = null)
    {
        HealthCheck.Enabled = true;
        configure?.Invoke(HealthCheck);
        return this;
    }
}

public class WebhookOptions
{
    public string Path { get; set; } = "/api/telegram/webhook/{botId}";
    public string? SecretToken { get; set; }
    public int MaxConnections { get; set; } = 40;
    public bool DropPendingUpdates { get; set; }

    public bool ValidateSecret(string? incomingToken)
    {
        if (string.IsNullOrEmpty(SecretToken))
            return true;

        return string.Equals(SecretToken, incomingToken, StringComparison.Ordinal);
    }
}

public class HealthCheckOptions
{
    public bool Enabled { get; set; }
    public TimeSpan PollTimeoutThreshold { get; set; } = TimeSpan.FromMinutes(5);
    public int UnhealthyAfterConsecutiveErrors { get; set; } = 10;
}
