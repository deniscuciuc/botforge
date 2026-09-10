namespace BotForge.Payments.Abstractions;

/// <summary>
/// Normalized representation of a purchase intent before any payment rail is executed.
/// All rails create and resolve a <see cref="PurchaseOrder"/> to ensure fulfillment
/// runs through a single idempotent path.
/// </summary>
public class PurchaseOrder
{
    /// <summary>Framework-assigned idempotency key.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>Telegram user id of the buyer.</summary>
    public required long UserId { get; init; }

    /// <summary>Chat id where fulfillment notifications should be sent.</summary>
    public required long ChatId { get; init; }

    /// <summary>Bot key used for this purchase.</summary>
    public required string BotId { get; init; }

    /// <summary>Application-defined payload that identifies the product/order on the consuming side.</summary>
    public required string Payload { get; init; }

    /// <summary>Expected amount in the smallest unit of <see cref="Currency"/>.</summary>
    public required long Amount { get; init; }

    /// <summary>Currency for the expected payment.</summary>
    public required PaymentCurrency Currency { get; init; }

    /// <summary>Rail through which payment is expected.</summary>
    public required PurchaseRail Rail { get; init; }

    /// <summary>Current settlement state.</summary>
    public SettlementStatus Status { get; set; } = SettlementStatus.Pending;

    /// <summary>Rail-specific settlement reference once a settlement event is received.</summary>
    public string? SettlementId { get; set; }

    /// <summary>When the order was created.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the order expires if payment is not received.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>When the settlement was confirmed.</summary>
    public DateTimeOffset? SettledAt { get; set; }

    /// <summary>Free-form metadata for rail-specific or application-specific data.</summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}
