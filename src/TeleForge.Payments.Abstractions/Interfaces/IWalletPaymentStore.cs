namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Persistence for wallet payment sessions and on-chain settlement records.
/// Implemented by the consuming application when wallet payment support is enabled.
/// </summary>
public interface IWalletPaymentStore
{
    // Sessions
    Task SaveSessionAsync(WalletPaymentSession session, CancellationToken ct = default);
    Task<WalletPaymentSession?> GetSessionAsync(string sessionId, CancellationToken ct = default);
    Task<WalletPaymentSession?> GetSessionByMemoAsync(string transferMemo, CancellationToken ct = default);
    Task<IReadOnlyList<WalletPaymentSession>> GetPendingSessionsAsync(int limit = 50, CancellationToken ct = default);

    Task UpdateSessionStatusAsync(string sessionId, SettlementStatus status, string? txHash, string? senderAddress,
        CancellationToken ct = default);
}
