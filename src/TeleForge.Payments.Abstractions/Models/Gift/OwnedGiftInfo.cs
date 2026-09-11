namespace TeleForge.Payments.Abstractions;

public abstract class OwnedGiftInfo
{
    public required string GiftId { get; init; }
    public required GiftType Type { get; init; }
    public long? SenderUserId { get; init; }
    public string? Text { get; init; }
    public DateTimeOffset? Date { get; init; }
    public bool IsSaved { get; init; }
}

public class OwnedGiftRegularInfo : OwnedGiftInfo
{
    public int StarCount { get; init; }
    public int? ConvertStarCount { get; init; }
    public int? PrepaidUpgradeStarCount { get; init; }
    public bool CanUpgrade { get; init; }
    public bool IsPrivate { get; init; }
}

public class OwnedGiftUniqueInfo : OwnedGiftInfo
{
    public required string Name { get; init; }
    public required int Number { get; init; }
    public string? Model { get; init; }
    public string? Backdrop { get; init; }
    public string? Symbol { get; init; }
    public bool CanTransfer { get; init; }
    public bool CanExportToBlockchain { get; init; }
    public int? TransferStarCount { get; init; }
}
