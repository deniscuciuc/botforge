using Telegram.Bot.Types;

namespace TeleForge.Routing.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class TelegramCommandScopeAttribute(TelegramCommandScopeKind scope = TelegramCommandScopeKind.Default)
    : Attribute
{
    public TelegramCommandScopeKind Scope { get; } = scope;
    public long ChatId { get; init; }
    public long UserId { get; init; }

    internal BotCommandScope ToBotCommandScope()
    {
        return Scope switch
        {
            TelegramCommandScopeKind.Default => new BotCommandScopeDefault(),
            TelegramCommandScopeKind.AllPrivateChats => new BotCommandScopeAllPrivateChats(),
            TelegramCommandScopeKind.AllGroupChats => new BotCommandScopeAllGroupChats(),
            TelegramCommandScopeKind.AllChatAdministrators => new BotCommandScopeAllChatAdministrators(),
            TelegramCommandScopeKind.Chat => ChatId == 0
                ? throw new InvalidOperationException("ChatId is required for TelegramCommandScopeKind.Chat.")
                : new BotCommandScopeChat { ChatId = ChatId },
            TelegramCommandScopeKind.ChatAdministrators => ChatId == 0
                ? throw new InvalidOperationException(
                    "ChatId is required for TelegramCommandScopeKind.ChatAdministrators.")
                : new BotCommandScopeChatAdministrators { ChatId = ChatId },
            TelegramCommandScopeKind.ChatMember => ChatId == 0 || UserId == 0
                ? throw new InvalidOperationException(
                    "ChatId and UserId are required for TelegramCommandScopeKind.ChatMember.")
                : new BotCommandScopeChatMember
                {
                    ChatId = ChatId,
                    UserId = UserId
                },
            _ => throw new ArgumentOutOfRangeException(nameof(Scope), Scope, "Unsupported command scope kind.")
        };
    }
}

public enum TelegramCommandScopeKind
{
    Default = 0,
    AllPrivateChats = 1,
    AllGroupChats = 2,
    AllChatAdministrators = 3,
    Chat = 4,
    ChatAdministrators = 5,
    ChatMember = 6
}
