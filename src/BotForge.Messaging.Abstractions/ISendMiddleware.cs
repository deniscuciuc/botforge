using BotForge.Core;

namespace BotForge.Messaging.Abstractions;

public delegate Task<SendResult> TelegramSendDelegate(SendContext context);

public interface ISendMiddleware
{
    Task<SendResult> InvokeAsync(SendContext context, TelegramSendDelegate next);
}

public class SendContext
{
    public QueuedTelegramMessage Message { get; init; } = null!;
    public BotConfiguration Bot { get; init; } = null!;
    public CancellationToken CancellationToken { get; set; }
    public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();
    public int Attempt { get; set; } = 1;
}
