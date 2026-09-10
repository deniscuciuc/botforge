namespace BotForge.Payments.Abstractions;

public class StarBalance
{
    public required int Amount { get; init; }
    public int? NanostarAmount { get; init; }
    public DateTimeOffset RetrievedAt { get; init; } = DateTimeOffset.UtcNow;
}
