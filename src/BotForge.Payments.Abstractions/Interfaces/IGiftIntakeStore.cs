namespace BotForge.Payments.Abstractions;

/// <summary>
/// Persistence for inbound gift intake records.
/// Implemented by the consuming application when inbound gift payment support is enabled.
/// </summary>
public interface IGiftIntakeStore
{
    Task SaveIntakeAsync(GiftIntakeRecord record, CancellationToken ct = default);
    Task<GiftIntakeRecord?> GetByOwnedGiftIdAsync(string ownedGiftId, CancellationToken ct = default);
    Task<IReadOnlyList<GiftIntakeRecord>> GetPendingIntakesAsync(int limit = 50, CancellationToken ct = default);

    Task UpdateIntakeStatusAsync(string id, GiftIntakeStatus status, string? purchaseOrderId,
        CancellationToken ct = default);
}
