using BotForge.Core;

namespace BotForge.Messaging.Abstractions;

public interface ISendPipeline
{
    Task<SendResult> SendAsync(SendContext context, CancellationToken ct = default);
}
