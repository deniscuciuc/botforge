using BotForge.Core;

namespace BotForge.Messaging.Abstractions;

public interface IMessageQueueBackend
{
    Task EnqueueAsync(QueuedTelegramMessage message, CancellationToken ct = default);
    Task EnqueueBatchAsync(IEnumerable<QueuedTelegramMessage> messages, CancellationToken ct = default);
}
