using BotForge.Core;

namespace BotForge.Messaging.Abstractions;

public interface ITelegramMessageService
{
    ITelegramMessage CreateMessage(string botId);
    ITelegramMessageBatch CreateBatch(string botId);
}

public interface ITelegramMessageBatch
{
    ITelegramMessageBatch Add(ITelegramMessage message);
    Task<IReadOnlyList<SendResult>> SendAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SendResult>> QueueAsync(CancellationToken ct = default);
}
