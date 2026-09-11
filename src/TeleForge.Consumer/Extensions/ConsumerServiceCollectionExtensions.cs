using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeleForge.Consumer.Hosting;
using TeleForge.Consumer.Polling;
using TeleForge.Consumer.Webhook;
using TeleForge.Core;

namespace TeleForge.Consumer.Extensions;

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
