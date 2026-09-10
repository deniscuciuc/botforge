namespace BotForge.Payments.Abstractions;

/// <summary>
/// Normalized settlement event produced by any payment rail after funds are confirmed.
/// Consumed by the fulfillment pipeline regardless of how the user paid.
/// </summary>
public class SettlementRecord
{
    /// <summary>Unique settlement id (rail-specific: charge id, tx hash, owned_gift_id, etc.).</summary>
    public required string SettlementId { get; init; }

    /// <summary>The purchase order this settlement corresponds to.</summary>
    public required string PurchaseOrderId { get; init; }

    /// <summary>Rail that produced this settlement.</summary>
    public required PurchaseRail Rail { get; init; }

    /// <summary>Telegram user who paid.</summary>
    public required long UserId { get; init; }

    /// <summary>Confirmed amount in smallest unit of <see cref="Currency"/>.</summary>
    public required long Amount { get; init; }

    /// <summary>Currency of the confirmed payment.</summary>
    public required PaymentCurrency Currency { get; init; }

    /// <summary>Current status of this settlement record.</summary>
    public SettlementStatus Status { get; set; } = SettlementStatus.Confirmed;

    /// <summary>When this settlement was recorded by the framework.</summary>
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Rail-specific raw payload preserved for diagnostics and manual review.</summary>
    public string? RawData { get; init; }
}
