namespace BotForge.Payments.Abstractions;

/// <summary>
/// Telegram Gifts API: send, list, convert, transfer, upgrade gifts.
/// Includes Business account gift operations when Business integration is enabled.
/// </summary>
public interface IGiftService
{
    Task<IReadOnlyList<GiftDefinition>> GetAvailableGiftsAsync(string botId, CancellationToken ct = default);
    Task SendGiftAsync(SendGiftRequest request, CancellationToken ct = default);

    Task<OwnedGiftListResult> GetUserGiftsAsync(string botId, long userId, string? offset = null, int? limit = null,
        CancellationToken ct = default);

    Task<GiftConversionResult> ConvertToStarsAsync(string botId, long userId, string ownedGiftId,
        CancellationToken ct = default);

    Task TransferGiftAsync(string botId, long senderUserId, string ownedGiftId, long targetUserId,
        int? starCount = null, CancellationToken ct = default);

    Task UpgradeToUniqueAsync(string botId, long userId, string ownedGiftId, CancellationToken ct = default);
    Task GiftPremiumAsync(string botId, long userId, int months, CancellationToken ct = default);

    // Business account operations (require can_view_gifts_and_stars / can_transfer_and_upgrade_gifts)

    /// <summary>
    /// Returns gifts owned by a connected Business account.
    /// Requires the <c>can_view_gifts_and_stars</c> business bot right.
    /// </summary>
    Task<OwnedGiftListResult> GetBusinessAccountGiftsAsync(string botId, string businessConnectionId,
        string? offset = null, int? limit = null, CancellationToken ct = default);

    /// <summary>
    /// Converts a regular gift owned by a Business account to Telegram Stars.
    /// Requires <c>can_convert_gifts_to_stars</c>.
    /// </summary>
    Task<GiftConversionResult> ConvertBusinessGiftToStarsAsync(string botId, string businessConnectionId,
        string ownedGiftId, CancellationToken ct = default);
}
