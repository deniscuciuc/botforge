using BotForge.Consumer.Extensions;
using BotForge.Core;
using BotForge.Messaging;
using BotForge.Messaging.MassTransit;
using BotForge.Observability;
using BotForge.Routing.Extensions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types.Enums;

namespace BotForge.Hosting;

public static class IngressHostExtensions
{
    /// <summary>
    /// Registers all framework services for the Ingress host profile.
    /// Includes: Consumer (polling/webhook), Routing pipeline, Messaging (send + queue),
    /// Observability, and Health checks.
    /// </summary>
    public static IServiceCollection AddTelegramIngressHost(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IngressHostOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new IngressHostOptions();
        configuration.GetSection("Telegram").Bind(options);
        configure?.Invoke(options);

        ConfigureMessaging(services, options.Bots, options.Queue);
        ConfigureRouting(services, options.HandlerAssemblies);
        ConfigureConsumer(services, options.Consumer, options.Webhook, options.Bots);
        ConfigureObservability(services, options.Observability);

        return services;
    }

    private static void ConfigureMessaging(
        IServiceCollection services,
        List<BotEntry> bots,
        QueueSettings queue)
    {
        services.AddTelegramMessaging(messaging =>
        {
            foreach (var bot in bots)
                messaging.AddBot(bot.Key, b =>
                {
                    b.Token = bot.Token;
                    b.Transport = Enum.TryParse<UpdateTransport>(bot.Transport, true, out var t)
                        ? t
                        : UpdateTransport.LongPolling;
                    b.AllowedUpdates = bot.AllowedUpdates
                        .Select(u => Enum.TryParse<UpdateType>(u, true, out var t) ? t : UpdateType.Unknown)
                        .Where(u => u != UpdateType.Unknown)
                        .ToArray();
                    b.RateLimit.GlobalPerSecond = bot.GlobalPerSecond;
                    b.RateLimit.PerChatPerSecond = bot.PerChatPerSecond;
                    b.RateLimit.GroupPerMinute = bot.GroupPerMinute;
                });

            messaging.UseSendMiddleware<Messaging.Middleware.RateLimitSendMiddleware>();
            messaging.UseSendMiddleware<Messaging.Middleware.RetrySendMiddleware>();
            messaging.UseSendMiddleware<Messaging.Middleware.CircuitBreakerSendMiddleware>();
            messaging.UseSendMiddleware<Messaging.Middleware.MetricsSendMiddleware>();
        });

        if (queue.Backend == QueueBackend.RabbitMq && !string.IsNullOrEmpty(queue.RabbitMqConnectionString))
        {
            services.AddTelegramMassTransitBackend();
            services.AddMassTransit(bus =>
            {
                bus.AddTelegramMessageConsumer();
                bus.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(queue.RabbitMqConnectionString);
                    cfg.ConfigureEndpoints(context);
                });
            });
        }
    }

    private static void ConfigureRouting(IServiceCollection services, List<string> handlerAssemblies)
    {
        services.AddTelegramRouting(routing =>
        {
            routing.AddHandlersFromAssembly(typeof(IngressHostExtensions).Assembly);

            foreach (var assemblyName in handlerAssemblies)
            {
                var assembly = System.Reflection.Assembly.Load(assemblyName);
                routing.AddHandlersFromAssembly(assembly);
            }
        });
    }

    private static void ConfigureConsumer(
        IServiceCollection services,
        ConsumerSettings consumer,
        WebhookSettings webhook,
        List<BotEntry> bots)
    {
        var hasWebhookBots = bots.Exists(b =>
            string.Equals(b.Transport, "Webhook", StringComparison.OrdinalIgnoreCase));

        services.AddTelegramConsumer(c =>
        {
            c.ConcurrencyLimit = consumer.ConcurrencyLimit;
            c.ChannelCapacity = consumer.ChannelCapacity;
            c.GracefulShutdownTimeout = TimeSpan.FromSeconds(consumer.GracefulShutdownSeconds);
            c.PollingLimit = consumer.PollingLimit;

            if (hasWebhookBots)
                c.ConfigureWebhook(w =>
                {
                    w.Path = webhook.Path;
                    w.SecretToken = webhook.SecretToken;
                    w.MaxConnections = webhook.MaxConnections;
                    w.DropPendingUpdates = webhook.DropPendingUpdates;
                });

            c.EnableHealthChecks();
        });
    }

    private static void ConfigureObservability(IServiceCollection services, ObservabilitySettings obs)
    {
        if (!obs.EnableMetrics) return;

        services.AddTelegramObservability(options =>
        {
            options.EnableMetrics = true;
            options.EnableMessagingMetrics = true;
            options.EnableRoutingMetrics = true;
            options.EnableConsumerMetrics = true;
            options.EnablePrometheusExporter = obs.EnablePrometheusExporter;

            if (obs.EnablePrometheusExporter)
                options.PrometheusUriPrefixes = [$"http://+:{obs.PrometheusPort}/metrics/"];
        });
    }
}
