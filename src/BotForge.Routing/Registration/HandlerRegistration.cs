using BotForge.Routing.Attributes;
using BotForge.Routing.Handlers;

namespace BotForge.Routing.Registration;

public class CommandRegistration
{
    public required string Command { get; init; }
    public string? BotId { get; init; }
    public required string Description { get; init; }
    public required Type HandlerType { get; init; }
    public required TelegramCommandAttribute CommandAttribute { get; init; }
    public TelegramCommandScopeAttribute[] CommandScopeAttributes { get; init; } = [];
    public AuthorizeAttribute[] AuthorizeAttributes { get; init; } = [];
    public RateLimitAttribute? RateLimitAttribute { get; init; }
    public ChatTypeAttribute? ChatTypeAttribute { get; init; }
}

public class CallbackRegistration
{
    public required string Pattern { get; init; }
    public required Type HandlerType { get; init; }
    public required CallbackQueryAttribute CallbackAttribute { get; init; }
    public AuthorizeAttribute[] AuthorizeAttributes { get; init; } = [];
    public RateLimitAttribute? RateLimitAttribute { get; init; }
    public bool IsRegex { get; init; }
}

public class TextMessageRegistration
{
    public required Type HandlerType { get; init; }
    public required TextMessageAttribute TextMessageAttribute { get; init; }
    public string? Pattern { get; init; }
    public int Priority { get; init; }
    public AuthorizeAttribute[] AuthorizeAttributes { get; init; } = [];
    public RateLimitAttribute? RateLimitAttribute { get; init; }
}

public class InlineQueryRegistration
{
    public required Type HandlerType { get; init; }
    public string? Pattern { get; init; }
}

public class ChosenInlineResultRegistration
{
    public required Type HandlerType { get; init; }
}

public class MediaRegistration
{
    public required Type HandlerType { get; init; }
    public required MediaType MediaType { get; init; }
}

public class LocationRegistration
{
    public required Type HandlerType { get; init; }
}

public class ContactRegistration
{
    public required Type HandlerType { get; init; }
}

public class UsersSharedRegistration
{
    public required Type HandlerType { get; init; }
}

public class ChatSharedRegistration
{
    public required Type HandlerType { get; init; }
}

public class PollAnswerRegistration
{
    public required Type HandlerType { get; init; }
}

public class ChatMemberRegistration
{
    public required Type HandlerType { get; init; }
    public bool MyChatMemberOnly { get; init; }
}

public class DiceRegistration
{
    public required Type HandlerType { get; init; }
    public string? Emoji { get; init; }
}

public class GiftMessageRegistration
{
    public required Type HandlerType { get; init; }
}

public class UniqueGiftMessageRegistration
{
    public required Type HandlerType { get; init; }
}
