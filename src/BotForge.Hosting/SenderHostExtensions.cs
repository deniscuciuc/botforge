using BotForge.Core;
using BotForge.Messaging;
using BotForge.Messaging.MassTransit;
using BotForge.Observability;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotForge.Hosting;

public static class SenderHostExtensions
{
    /// <summary>
    /// Registers all framework services for the Sender host profile.
    /// Includes: Messaging (send pipeline), MassTransit consumer, Observability, Health checks.
    /// The sender listens on the queue for <see cref="QueuedTelegramMessage"/> and delivers them via Telegram API.
    /// </summary>
    public static IServiceCollection AddTelegramSenderHost(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<SenderHostOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new SenderHostOptions();
        configuration.GetSection("Telegram").Bind(options);
        configure?.Invoke(options);

        ConfigureMessaging(services, options.Bots, options.Send);
        ConfigureQueue(services, options.Queue);
        ConfigureObservability(services, options.Observability);

        return services;
    }

    private static void ConfigureMessaging(
        IServiceCollection services,
        List<BotEntry> bots,
        SendSettings send)
    {
        services.AddTelegramMessaging(messaging =>
        {
            foreach (var bot in bots)
                messaging.AddBot(bot.Key, b =>
                {
                    b.Token = bot.Token;
                    b.Transport = UpdateTransport.LongPolling; // Sender doesn't receive updates
                    b.RateLimit.GlobalPerSecond = bot.GlobalPerSecond;
                    b.RateLimit.PerChatPerSecond = bot.PerChatPerSecond;
                    b.RateLimit.GroupPerMinute = bot.GroupPerMinute;
                });

            messaging.RetryCount = send.RetryCount;
            messaging.RetryBaseDelay = TimeSpan.FromSeconds(send.RetryBaseDelaySeconds);
            messaging.MaxRetryDelay = TimeSpan.FromSeconds(send.MaxRetryDelaySeconds);
            messaging.CircuitBreaker.FailureThreshold = send.CircuitBreakerFailureThreshold;
            messaging.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(send.CircuitBreakerBreakDurationSeconds);
            messaging.CircuitBreaker.UseRetryAfterForBreakDuration = send.UseRetryAfterForBreakDuration;

            messaging.UseSendMiddleware<Messaging.Middleware.RateLimitSendMiddleware>();
            messaging.UseSendMiddleware<Messaging.Middleware.RetrySendMiddleware>();
            messaging.UseSendMiddleware<Messaging.Middleware.CircuitBreakerSendMiddleware>();
            messaging.UseSendMiddleware<Messaging.Middleware.MetricsSendMiddleware>();
        });
    }

    private static void ConfigureQueue(IServiceCollection services, QueueSettings queue)
    {
        services.AddTelegramMassTransitBackend();

        if (queue.Backend == QueueBackend.RabbitMq && !string.IsNullOrEmpty(queue.RabbitMqConnectionString))
            services.AddMassTransit(bus =>
            {
                bus.AddTelegramMessageConsumer();
                bus.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(queue.RabbitMqConnectionString);
                    cfg.ConfigureEndpoints(context);
                });
            });
        else
            services.AddMassTransit(bus =>
            {
                bus.AddTelegramMessageConsumer();
                bus.UsingInMemory((context, cfg) => { cfg.ConfigureEndpoints(context); });
            });
    }

    private static void ConfigureObservability(IServiceCollection services, ObservabilitySettings obs)
    {
        if (!obs.EnableMetrics) return;

        services.AddTelegramObservability(options =>
        {
            options.EnableMetrics = true;
            options.EnableMessagingMetrics = true;
            options.EnablePrometheusExporter = obs.EnablePrometheusExporter;

            if (obs.EnablePrometheusExporter)
                options.PrometheusUriPrefixes = [$"http://+:{obs.PrometheusPort}/metrics/"];
        });
    }
}
