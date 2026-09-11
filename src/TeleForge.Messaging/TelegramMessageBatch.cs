using TeleForge.Core;
using TeleForge.Messaging.Abstractions;

namespace TeleForge.Messaging;

public class TelegramMessageBatch(
    string botId,
    IMessageQueueBackend? queueBackend,
    ISendPipeline sendPipeline,
    ITelegramBotClientProvider botProvider)
    : ITelegramMessageBatch
{
    private readonly List<ITelegramMessage> _messages = [];

    public ITelegramMessageBatch Add(ITelegramMessage message)
    {
        _messages.Add(message);
        return this;
    }

    public async Task<IReadOnlyList<SendResult>> SendAsync(CancellationToken ct = default)
    {
        var results = new List<SendResult>(_messages.Count);
        var botConfig = botProvider.GetConfiguration(botId);

        foreach (var context in _messages.Select(msg => msg.Build()).Select(queued => new SendContext
        {
            Message = queued,
            Bot = botConfig,
            CancellationToken = ct
        }))
        {
            var result = await sendPipeline.SendAsync(context, ct).ConfigureAwait(false);
            results.Add(result);
        }

        return results;
    }

    public async Task<IReadOnlyList<SendResult>> QueueAsync(CancellationToken ct = default)
    {
        if (queueBackend == null)
            return await SendAsync(ct).ConfigureAwait(false);

        var messages = _messages.Select(m => m.Build()).ToList();
        await queueBackend.EnqueueBatchAsync(messages, ct).ConfigureAwait(false);

        return messages.Select(_ => new SendResult { Success = true }).ToList();
    }
}
