using Telegram.Bot.Types.Enums;

namespace BotForge.Payments.Abstractions;

public class SendGiftRequest
{
    public required long UserId { get; init; }
    public required string GiftId { get; init; }
    public required string BotId { get; init; }
    public string? Text { get; init; }
    public ParseMode? TextParseMode { get; init; }
    public bool PayForUpgrade { get; init; }
}
