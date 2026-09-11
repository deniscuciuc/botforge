using TeleForge.Core;
using TeleForge.Messaging.Abstractions;
using TeleForge.RateLimiting.Abstractions;

namespace TeleForge.Messaging;

public class TelegramMessagingOptions
{
    public bool LogHttpRequests { get; set; } = true;
    public MessagePriority DefaultPriority { get; set; } = MessagePriority.Normal;
    public int RetryCount { get; set; } = 3;
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(60);
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    internal Dictionary<string, BotConfiguration> Bots { get; } = new();
    internal List<Type> SendMiddleware { get; } = [];
    internal Type? QueueBackendType { get; set; }
    internal Type? RateLimitStoreType { get; set; }

    public TelegramMessagingOptions AddBot(string key, Action<BotConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var config = new BotConfiguration { Key = key };
        configure(config);
        Bots[key] = config;
        return this;
    }

    public TelegramMessagingOptions UseSendMiddleware<T>() where T : class, ISendMiddleware
    {
        SendMiddleware.Add(typeof(T));
        return this;
    }

    public TelegramMessagingOptions UseQueueBackend<T>() where T : class, IMessageQueueBackend
    {
        QueueBackendType = typeof(T);
        return this;
    }

    public TelegramMessagingOptions UseRateLimitStore<T>() where T : class, IRateLimitStore
    {
        RateLimitStoreType = typeof(T);
        return this;
    }
}

public class CircuitBreakerOptions
{
    public int FailureThreshold { get; set; } = 10;
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);
    public bool UseRetryAfterForBreakDuration { get; set; } = true;
}
