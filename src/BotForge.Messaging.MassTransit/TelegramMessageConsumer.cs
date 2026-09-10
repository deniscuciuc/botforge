using BotForge.Core;
using BotForge.Messaging.Abstractions;
using MassTransit;
using Microsoft.Extensions.Logging;
using SendContext = BotForge.Messaging.Abstractions.SendContext;

namespace BotForge.Messaging.MassTransit;

/// <summary>
/// MassTransit consumer that receives <see cref="QueuedTelegramMessage"/> from the bus
/// and sends them through the send pipeline.
/// </summary>
public class TelegramMessageConsumer(
    ISendPipeline sendPipeline,
    ILogger<TelegramMessageConsumer> logger) : IConsumer<QueuedTelegramMessage>
{
    public async Task Consume(ConsumeContext<QueuedTelegramMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;

        logger.LogDebug("Consuming message {MessageId} for chat {ChatId} via MassTransit",
            message.Id, message.ChatId);

        var sendContext = new SendContext { Message = message };
        var result = await sendPipeline.SendAsync(sendContext, context.CancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            logger.LogWarning("Failed to send message {MessageId}: {Error}",
                message.Id, result.Error);

            if (result.RetryAfter.HasValue)
                // Schedule retry via MassTransit delayed redelivery
                throw new TelegramRateLimitedException(result.RetryAfter.Value);
        }
    }
}
