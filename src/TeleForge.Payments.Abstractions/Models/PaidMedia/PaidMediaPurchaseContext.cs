namespace TeleForge.Payments.Abstractions;

public class PaidMediaPurchaseContext
{
    public required string BotId { get; init; }
    public required long UserId { get; init; }
    public required string Payload { get; init; }

    /// <summary>
    /// Arbitrary data bag for passing data between handlers.
    /// </summary>
    public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();
}
