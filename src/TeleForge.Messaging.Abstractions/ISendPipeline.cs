using TeleForge.Core;

namespace TeleForge.Messaging.Abstractions;

public interface ISendPipeline
{
    Task<SendResult> SendAsync(SendContext context, CancellationToken ct = default);
}
