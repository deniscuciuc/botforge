namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Initiates and tracks wallet-based payment sessions (TON Connect, direct-address, custodial).
/// </summary>
public interface IWalletPaymentService
{
    /// <summary>
    /// Creates a new wallet payment session for the given request.
    /// Returns a result containing the session and an optional payment URL for the Mini App.
    /// </summary>
    Task<WalletPaymentResult> CreateSessionAsync(WalletPaymentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Looks up an active session by id for status polling.
    /// </summary>
    Task<WalletPaymentSession?> GetSessionAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Called by the blockchain observer or custodial webhook when a transfer is confirmed.
    /// Validates the transfer matches the session and triggers settlement.
    /// </summary>
    Task<SettlementRecord?> RecordTransferAsync(string transferMemo, string txHash, string senderAddress,
        long actualAmount, PaymentCurrency currency, CancellationToken ct = default);
}
