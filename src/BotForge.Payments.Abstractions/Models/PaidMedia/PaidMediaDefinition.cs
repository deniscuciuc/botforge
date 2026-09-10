using Telegram.Bot.Types;

namespace BotForge.Payments.Abstractions;

public class PaidMediaDefinition
{
    public required long ChatId { get; init; }
    public required string BotId { get; init; }
    public required int StarCount { get; init; }
    public required IReadOnlyList<InputPaidMedia> Media { get; init; }
    public string? Caption { get; init; }
    public string? ParseMode { get; init; }
    public string? BusinessConnectionId { get; init; }
    public bool? ShowCaptionAboveMedia { get; init; }
    public bool DisableNotification { get; init; }
    public bool ProtectContent { get; init; }
    public int? ReplyToMessageId { get; init; }
    public string? Payload { get; init; }
}
