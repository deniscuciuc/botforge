using Telegram.Bot.Types;

namespace BotForge.Core;

public sealed class RenderedMessageText
{
    public string Text { get; init; } = string.Empty;
    public IReadOnlyList<MessageEntity>? Entities { get; init; }
    public bool HasEntities => Entities is { Count: > 0 };
}
