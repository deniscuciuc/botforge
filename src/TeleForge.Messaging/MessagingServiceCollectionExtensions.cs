using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeleForge.Core;
using TeleForge.Messaging.Abstractions;
using TeleForge.Messaging.Commands;
using TeleForge.Messaging.Middleware;
using TeleForge.RateLimiting.Abstractions;

namespace TeleForge.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramMessaging(
        this IServiceCollection services,
        Action<TelegramMessagingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new TelegramMessagingOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddHttpClient();
        foreach (var botKey in options.Bots.Keys)
        {
            var botHttpClient = services.AddHttpClient($"TelegramBot_{botKey}");
            if (!options.LogHttpRequests)
                botHttpClient.RemoveAllLoggers();
        }
        services.AddSingleton<ITelegramBotClientProvider, TelegramBotClientProvider>();
        services.AddSingleton<ITelegramCommandSyncService, TelegramCommandSyncService>();
        services.AddSingleton<TelegramApiTransport>();
        services.AddSingleton<ISendPipeline, SendPipeline>();
        services.AddSingleton<ITelegramMessageService, TelegramMessageService>();

        if (options.RateLimitStoreType != null)
            services.TryAddSingleton(typeof(IRateLimitStore), options.RateLimitStoreType);
        else
            services.TryAddSingleton<IRateLimitStore, InMemoryRateLimitStore>();

        if (options.QueueBackendType != null)
            services.TryAddSingleton(typeof(IMessageQueueBackend), options.QueueBackendType);

        foreach (var middlewareType in options.SendMiddleware)
            services.TryAddSingleton(middlewareType);

        services.TryAddSingleton<RateLimitSendMiddleware>();
        services.TryAddSingleton<MetricsSendMiddleware>();
        services.TryAddSingleton<RetrySendMiddleware>();
        services.TryAddSingleton<CircuitBreakerSendMiddleware>();

        return services;
    }
}
