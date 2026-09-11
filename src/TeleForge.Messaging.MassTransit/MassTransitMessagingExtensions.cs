using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeleForge.Messaging.Abstractions;

namespace TeleForge.Messaging.MassTransit;

public static class MassTransitMessagingExtensions
{
    /// <summary>
    /// Registers MassTransit as the message queue backend for Telegram messaging.
    /// </summary>
    public static IServiceCollection AddTelegramMassTransitBackend(this IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton<IMessageQueueBackend, MassTransitQueueBackend>());
        return services;
    }

    /// <summary>
    /// Adds the Telegram message consumer to a MassTransit bus configuration.
    /// </summary>
    public static void AddTelegramMessageConsumer(this IBusRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.AddConsumer<TelegramMessageConsumer>();
    }
}
