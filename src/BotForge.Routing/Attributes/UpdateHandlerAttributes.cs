using BotForge.Routing.Handlers;

namespace BotForge.Routing.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class InlineQueryAttribute(string? pattern = null) : Attribute
{
    /// <summary>Optional regex pattern to match against the inline query text.</summary>
    public string? Pattern { get; } = pattern;
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class ChosenInlineResultAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class)]
public sealed class MediaMessageAttribute(MediaType mediaType) : Attribute
{
    public MediaType MediaType { get; } = mediaType;
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class LocationMessageAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ContactMessageAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class)]
public sealed class UsersSharedMessageAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ChatSharedMessageAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PollAnswerAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ChatMemberAttribute : Attribute
{
    /// <summary>If true, only handles MyChatMember updates (bot added/removed). Otherwise handles ChatMember updates.</summary>
    public bool MyChatMemberOnly { get; init; }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class DiceMessageAttribute(string? emoji = null) : Attribute
{
    /// <summary>Optional emoji filter (🎲, 🎯, 🏀, ⚽, 🎰, 🎳). Null means any dice.</summary>
    public string? Emoji { get; } = emoji;
}

/// <summary>Marks a class as a handler for regular gift service messages (Message.Gift).</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GiftMessageAttribute : Attribute;

/// <summary>Marks a class as a handler for unique gift service messages (Message.UniqueGift).</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class UniqueGiftMessageAttribute : Attribute;
