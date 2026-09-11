namespace TeleForge.Payments.Abstractions;

public class RevenueSnapshot
{
    public required StarBalance Balance { get; init; }
    public int TotalIncomingTransactions { get; init; }
    public int TotalOutgoingTransactions { get; init; }
    public DateTimeOffset SnapshotAt { get; init; } = DateTimeOffset.UtcNow;
}
