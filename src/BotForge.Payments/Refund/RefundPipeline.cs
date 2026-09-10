using BotForge.Payments.Abstractions;
using BotForge.Payments.Router;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BotForge.Payments.Refund;

/// <summary>
/// Runs the chain of <see cref="IRefundProcessor"/> instances for a Telegram-initiated refund.
/// Ensures idempotent processing via <see cref="IPaymentStore"/>.
/// </summary>
public class RefundPipeline(
    IServiceProvider services,
    IPaymentMetrics metrics,
    ILogger<RefundPipeline> logger)
{
    public async Task HandleAsync(RefundContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var store = services.GetService<IPaymentStore>();
        if (store != null)
        {
            var alreadyRecorded = await store.IsRefundRecordedAsync(context.ChargeId, ct).ConfigureAwait(false);
            if (alreadyRecorded)
            {
                logger.LogWarning("Duplicate refund ignored for charge {ChargeId}", context.ChargeId);
                return;
            }
        }

        try
        {
            var registry = services.GetRequiredService<PaymentHandlerRegistry>();
            var processors = registry.GetRefundProcessors(context.PayloadPrefix, services);

            RefundDecision? lastDecision = null;
            foreach (var processor in processors) lastDecision = await processor.ProcessAsync(context, ct).ConfigureAwait(false);

            if (store != null)
                await store.RecordRefundAsync(new RefundRecord
                {
                    ChargeId = context.ChargeId,
                    UserId = context.UserId,
                    Amount = (int)context.TotalAmount,
                    Currency = context.Currency,
                    Payload = context.RawPayload,
                    Reason = lastDecision?.Reason ?? RefundReason.TelegramInitiated,
                    BalanceReversed = lastDecision?.ReverseBalance ?? false,
                    Description = lastDecision?.Description
                }, ct).ConfigureAwait(false);

            metrics.RefundProcessed(context.BotId, lastDecision?.Reason ?? RefundReason.TelegramInitiated,
                (int)context.TotalAmount);
            logger.LogInformation(
                "Refund processed: user {UserId}, prefix {Prefix}, charge {ChargeId}, amount {Amount}, reversed {Reversed}",
                context.UserId, context.PayloadPrefix, context.ChargeId, context.TotalAmount,
                lastDecision?.ReverseBalance ?? false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Refund processing failed: user {UserId}, prefix {Prefix}, charge {ChargeId}",
                context.UserId, context.PayloadPrefix, context.ChargeId);
            throw;
        }
    }
}
