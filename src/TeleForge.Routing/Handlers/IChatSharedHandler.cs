using TeleForge.Core;
using Telegram.Bot.Types;

namespace TeleForge.Routing.Handlers;

public interface IChatSharedHandler
{
    Task HandleAsync(ChatSharedContext context, CancellationToken ct);
}

public class ChatSharedContext(TelegramUpdateContext updateContext, ChatShared chatShared)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public ChatShared ChatShared { get; } = chatShared;
    public int RequestId => ChatShared.RequestId;
    public long ChatIdValue => ChatShared.ChatId;
    public string? Title => ChatShared.Title;
    public string? Username => ChatShared.Username;
    public long? ChatId => UpdateContext.ChatId;
    public long? UserId => UpdateContext.UserId;
    public string BotId => UpdateContext.BotId;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;
}
