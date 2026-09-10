namespace BotForge.Payments.Abstractions;

public class GiftDefinition
{
    public required string Id { get; init; }
    public required int StarCount { get; init; }
    public int? TotalCount { get; init; }
    public int? RemainingCount { get; init; }
    public string? StickerId { get; init; }
}
