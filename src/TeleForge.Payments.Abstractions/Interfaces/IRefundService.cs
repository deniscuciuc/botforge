namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Refunds a successful payment via Telegram's RefundStarPayment API.
/// </summary>
public interface IRefundService
{
    Task<bool> RefundStarPaymentAsync(string botId, long userId, string telegramPaymentChargeId,
        CancellationToken ct = default);
}
