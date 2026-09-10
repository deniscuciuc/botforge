using BotForge.Consumer.Hosting;
using BotForge.Consumer.Polling;
using BotForge.Consumer.Webhook;
using BotForge.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BotForge.Consumer.Extensions;

public static class ConsumerServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramConsumer(
        this IServiceCollection services,
        Action<TelegramConsumerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new TelegramConsumerOptions();
        configure(options);

        var channel = new UpdateChannel(options.ChannelCapacity);

        services.AddSingleton(options);
        services.AddSingleton(channel);
        services.AddSingleton<TelegramUpdateWorker>();
        services.AddSingleton<LongPollingConsumer>();

        services.AddSingleton(new WebhookUpdateHandler(channel, options.Webhook));

        services.AddSingleton<IHostedService>(sp => new TelegramConsumerHostedService(
            options,
            sp.GetRequiredService<ITelegramBotClientProvider>(),
            channel,
            sp.GetRequiredService<TelegramUpdateWorker>(),
            sp.GetRequiredService<LongPollingConsumer>(),
            sp.GetRequiredService<ILogger<TelegramConsumerHostedService>>()));

        return services;
    }
}
