namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Record of a Telegram gift received by the bot (via Business account or service-message route)
/// that is being evaluated or accepted as an inbound payment.
/// </summary>
public class GiftIntakeRecord
{
    /// <summary>Framework-assigned id for this intake event.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>Telegram's owned_gift_id for the received gift.</summary>
    public required string OwnedGiftId { get; init; }

    /// <summary>Catalog gift id.</summary>
    public required string GiftId { get; init; }

    /// <summary>Type of gift (Regular or Unique).</summary>
    public required GiftType GiftType { get; init; }

    /// <summary>Telegram user who sent the gift.</summary>
    public long? SenderUserId { get; init; }

    /// <summary>Star value the gift can be converted to (if known).</summary>
    public int? ConvertStarCount { get; init; }

    /// <summary>Whether the gift can be upgraded to a unique one.</summary>
    public bool CanBeUpgraded { get; init; }

    /// <summary>Status of this intake (Pending evaluation / Accepted / Rejected).</summary>
    public GiftIntakeStatus Status { get; set; } = GiftIntakeStatus.Pending;

    /// <summary>Purchase order this gift was accepted as payment for (if matched).</summary>
    public string? PurchaseOrderId { get; set; }

    /// <summary>When Telegram reported the gift was sent.</summary>
    public DateTimeOffset GiftedAt { get; init; }

    /// <summary>When this intake record was created by the framework.</summary>
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Processing status of an inbound gift.
/// </summary>
public enum GiftIntakeStatus
{
    /// <summary>Gift received; waiting to be matched to a purchase order.</summary>
    Pending,

    /// <summary>Gift matched to a purchase order and accepted as payment.</summary>
    Accepted,

    /// <summary>Gift rejected (e.g., wrong type, wrong value, no matching order).</summary>
    Rejected,

    /// <summary>Gift converted to Stars after acceptance.</summary>
    ConvertedToStars
}
