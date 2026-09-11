namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Processes inbound gift events (received via service messages or Business account polling).
/// Matches them to pending purchase orders and emits settlement records.
/// </summary>
public interface IGiftIntakeService
{
    /// <summary>
    /// Process a received gift from a Telegram service message (Message.gift or Message.unique_gift).
    /// Only effective when an <see cref="IGiftIntakeStore"/> and <see cref="ICommerceStore"/> are registered.
    /// </summary>
    Task ProcessReceivedGiftAsync(ReceivedGiftEvent giftEvent, CancellationToken ct = default);

    /// <summary>
    /// Reconcile business account gifts by polling the owned gifts list.
    /// Used to catch any gifts missed by real-time service messages.
    /// </summary>
    Task ReconcileBusinessGiftsAsync(string botId, string businessConnectionId, CancellationToken ct = default);
}

/// <summary>
/// Describes a gift received by the bot or its connected Business account.
/// </summary>
public class ReceivedGiftEvent
{
    /// <summary>Bot key this event arrived on.</summary>
    public required string BotId { get; init; }

    /// <summary>Telegram unique identifier of the received gift (owned_gift_id for Business; or
    /// derived from the service message for regular bot receipts).</summary>
    public required string OwnedGiftId { get; init; }

    /// <summary>Catalog gift id.</summary>
    public required string GiftId { get; init; }

    /// <summary>Gift type (Regular or Unique).</summary>
    public required GiftType GiftType { get; init; }

    /// <summary>User id of the sender (may be null if private/hidden).</summary>
    public long? SenderUserId { get; init; }

    /// <summary>Stars value this gift can be converted to (if known).</summary>
    public int? ConvertStarCount { get; init; }

    /// <summary>True when the gift can be upgraded to a unique gift.</summary>
    public bool CanBeUpgraded { get; init; }

    /// <summary>Timestamp when the gift was sent.</summary>
    public required DateTimeOffset GiftedAt { get; init; }

    /// <summary>True when raised via Business account polling (vs. real-time service message).</summary>
    public bool IsFromPolling { get; init; }

    /// <summary>Business connection id if this gift was received on a Business account.</summary>
    public string? BusinessConnectionId { get; init; }
}
