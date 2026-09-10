using BotForge.Core;
using BotForge.Messaging.Abstractions;

namespace BotForge.Messaging;

public class TelegramMessageService(
    ISendPipeline sendPipeline,
    ITelegramBotClientProvider botProvider,
    IMessageQueueBackend? queueBackend = null)
    : ITelegramMessageService
{
    public ITelegramMessage CreateMessage(string botId)
    {
        return new TelegramMessageBuilder(
            botId,
            sendPipeline,
            queueBackend,
            botProvider);
    }

    public ITelegramMessageBatch CreateBatch(string botId)
    {
        return new TelegramMessageBatch(botId, queueBackend, sendPipeline, botProvider);
    }
}
