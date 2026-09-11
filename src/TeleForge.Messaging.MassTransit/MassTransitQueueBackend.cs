using MassTransit;
using Microsoft.Extensions.Logging;
using TeleForge.Core;
using TeleForge.Messaging.Abstractions;

namespace TeleForge.Messaging.MassTransit;

/// <summary>
/// MassTransit-backed message queue backend. Publishes outgoing Telegram messages
/// to the bus for distributed processing by <see cref="TelegramMessageConsumer"/>.
/// </summary>
public class MassTransitQueueBackend(
    IPublishEndpoint publishEndpoint,
    ILogger<MassTransitQueueBackend> logger) : IMessageQueueBackend
{
    public async Task EnqueueAsync(QueuedTelegramMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        logger.LogDebug("Publishing message {MessageId} to MassTransit for chat {ChatId}",
            message.Id, message.ChatId);

        await publishEndpoint.Publish(message, ct).ConfigureAwait(false);
    }

    public async Task EnqueueBatchAsync(IEnumerable<QueuedTelegramMessage> messages, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        foreach (var message in messages) await publishEndpoint.Publish(message, ct).ConfigureAwait(false);
    }
}
