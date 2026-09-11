using TeleForge.Core;
using Telegram.Bot.Types;

namespace TeleForge.Routing.Handlers;

public interface IChatMemberHandler
{
    Task HandleAsync(ChatMemberContext context, CancellationToken ct);
}

public class ChatMemberContext(
    TelegramUpdateContext updateContext,
    ChatMemberUpdated chatMemberUpdated,
    bool isMyChatMember)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public ChatMemberUpdated ChatMemberUpdated { get; } = chatMemberUpdated;
    public bool IsMyChatMember { get; } = isMyChatMember;
    public ChatMember OldChatMember => ChatMemberUpdated.OldChatMember;
    public ChatMember NewChatMember => ChatMemberUpdated.NewChatMember;
    public long? ChatId => UpdateContext.ChatId;
    public long? UserId => UpdateContext.UserId;
    public string BotId => UpdateContext.BotId;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;
}
