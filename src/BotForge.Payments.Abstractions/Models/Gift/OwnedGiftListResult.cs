namespace BotForge.Payments.Abstractions;

public class OwnedGiftListResult
{
    public required IReadOnlyList<OwnedGiftInfo> Gifts { get; init; }
    public int TotalCount { get; init; }
    public string? NextOffset { get; init; }
}
