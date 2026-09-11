using TeleForge.Core;

namespace TeleForge.Messaging.Abstractions;

public interface IMessageQueueBackend
{
    Task EnqueueAsync(QueuedTelegramMessage message, CancellationToken ct = default);
    Task EnqueueBatchAsync(IEnumerable<QueuedTelegramMessage> messages, CancellationToken ct = default);
}
