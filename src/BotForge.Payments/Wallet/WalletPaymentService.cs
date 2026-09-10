using BotForge.Payments.Abstractions;
using BotForge.Payments.Commerce;
using Microsoft.Extensions.Logging;

namespace BotForge.Payments.Wallet;

/// <summary>
/// Core wallet payment service: creates sessions, retrieves them, and handles transfer confirmation.
/// This rail-agnostic layer sits between the per-rail TON services and the fulfillment pipeline.
/// TON Connect, direct-address, and custodial wallet services all funnel through here once a
/// transfer is observed.
/// </summary>
public class WalletPaymentService(
    IWalletPaymentStore walletStore,
    ICommerceStore commerceStore,
    PurchaseFulfillmentPipeline fulfillmentPipeline,
    ILogger<WalletPaymentService> logger)
    : IWalletPaymentService
{
    public async Task<WalletPaymentResult> CreateSessionAsync(WalletPaymentRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var memo = GenerateMemo(request.PurchaseOrderId, request.UserId);

        var session = new WalletPaymentSession
        {
            PurchaseOrderId = request.PurchaseOrderId,
            UserId = request.UserId,
            Rail = request.Rail,
            ExpectedAmount = request.Amount,
            Currency = request.Currency,
            TransferMemo = memo,
            ExpiresAt = DateTimeOffset.UtcNow.Add(request.Expiry)
        };

        await walletStore.SaveSessionAsync(session, ct).ConfigureAwait(false);

        logger.LogInformation(
            "Wallet session {SessionId} created for order {OrderId} via {Rail}",
            session.SessionId, request.PurchaseOrderId, request.Rail);

        return WalletPaymentResult.Ok(session);
    }

    public Task<WalletPaymentSession?> GetSessionAsync(string sessionId, CancellationToken ct = default)
    {
        return walletStore.GetSessionAsync(sessionId, ct);
    }

    public async Task<SettlementRecord?> RecordTransferAsync(string transferMemo, string txHash,
        string senderAddress, long actualAmount, PaymentCurrency currency, CancellationToken ct = default)
    {
        var session = await walletStore.GetSessionByMemoAsync(transferMemo, ct).ConfigureAwait(false);

        if (session is null)
        {
            logger.LogWarning("No wallet session found for memo {Memo}", transferMemo);
            return null;
        }

        if (session.Status != SettlementStatus.Pending)
        {
            logger.LogDebug("Session {SessionId} is already in status {Status}, skipping", session.SessionId,
                session.Status);
            return null;
        }

        if (session.ExpiresAt < DateTimeOffset.UtcNow)
        {
            logger.LogWarning("Session {SessionId} expired at {ExpiresAt}", session.SessionId, session.ExpiresAt);
            await walletStore.UpdateSessionStatusAsync(session.SessionId, SettlementStatus.Expired, null, null, ct).ConfigureAwait(false);
            return null;
        }

        if (session.Currency != currency)
        {
            logger.LogWarning(
                "Currency mismatch for session {SessionId}: expected {Expected}, got {Actual}",
                session.SessionId, session.Currency, currency);
            await walletStore.UpdateSessionStatusAsync(session.SessionId, SettlementStatus.Failed, null, null, ct).ConfigureAwait(false);
            return null;
        }

        if (actualAmount < session.ExpectedAmount)
        {
            logger.LogWarning(
                "Underpayment for session {SessionId}: expected {Expected}, got {Actual}",
                session.SessionId, session.ExpectedAmount, actualAmount);
            await walletStore.UpdateSessionStatusAsync(session.SessionId, SettlementStatus.Failed, null, null, ct).ConfigureAwait(false);
            return null;
        }

        session.TransactionHash = txHash;
        session.SenderAddress = senderAddress;
        await walletStore.UpdateSessionStatusAsync(session.SessionId, SettlementStatus.Confirmed, txHash, senderAddress,
            ct).ConfigureAwait(false);

        var order = await commerceStore.GetOrderAsync(session.PurchaseOrderId, ct).ConfigureAwait(false);
        if (order is null)
        {
            logger.LogError("Order {OrderId} not found for confirmed session {SessionId}",
                session.PurchaseOrderId, session.SessionId);
            return null;
        }

        var settlement = new SettlementRecord
        {
            SettlementId = $"tx:{txHash}",
            PurchaseOrderId = order.Id,
            Rail = session.Rail,
            UserId = session.UserId,
            Amount = actualAmount,
            Currency = currency,
            Status = SettlementStatus.Confirmed,
            RawData = txHash
        };

        await fulfillmentPipeline.FulfillAsync(order, settlement, ct).ConfigureAwait(false);

        logger.LogInformation(
            "Transfer {TxHash} confirmed for session {SessionId}, order {OrderId} fulfilled",
            txHash, session.SessionId, order.Id);

        return settlement;
    }

    /// <summary>
    /// Produces a short, unique memo string that fits in a TON transfer comment (≤128 bytes).
    /// Format: first 8 chars of orderId + first 6 chars of userId base36.
    /// </summary>
    private static string GenerateMemo(string purchaseOrderId, long userId)
    {
        var prefix = purchaseOrderId.Replace("-", "").AsSpan(0, Math.Min(8, purchaseOrderId.Replace("-", "").Length));
        var userTag = Convert.ToString(userId % 16_777_216L, 16).PadLeft(6, '0');
        return $"tfw{prefix}{userTag}";
    }
}
