using Microsoft.Extensions.Logging;
using TeleForge.Core;
using TeleForge.Payments.Abstractions;
using Telegram.Bot;

namespace TeleForge.Payments.Refund;

/// <summary>
/// Service for programmatically issuing refunds via Telegram's RefundStarPayment API.
/// </summary>
public class RefundService(
    ITelegramBotClientProvider botProvider,
    IPaymentMetrics metrics,
    ILogger<RefundService> logger)
    : IRefundService
{
    public async Task<bool> RefundStarPaymentAsync(string botId, long userId, string telegramPaymentChargeId,
        CancellationToken ct = default)
    {
        try
        {
            var client = botProvider.GetClient(botId);
            await client.RefundStarPayment(userId, telegramPaymentChargeId, ct).ConfigureAwait(false);

            metrics.RefundProcessed(botId, RefundReason.Admin, 0);
            logger.LogInformation("Star payment refunded: user {UserId}, charge {ChargeId}", userId,
                telegramPaymentChargeId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refund Star payment: user {UserId}, charge {ChargeId}", userId,
                telegramPaymentChargeId);
            return false;
        }
    }
}
