using BotForge.Core;
using BotForge.Payments.Abstractions;
using BotForge.Payments.Router;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace BotForge.Payments.Checkout;

/// <summary>
/// Runs the chain of <see cref="IPaymentProcessor"/> instances for a successful payment.
/// On failure, optionally auto-refunds the payment.
/// </summary>
public class PaymentProcessingPipeline(
    ITelegramBotClientProvider botProvider,
    IServiceProvider services,
    IPaymentMetrics metrics,
    Microsoft.Extensions.Options.IOptions<PaymentOptions> options,
    ILogger<PaymentProcessingPipeline> logger)
{
    private readonly PaymentOptions _options = options.Value;

    public async Task HandleAsync(SuccessfulPaymentContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        using var _ = metrics.MeasurePaymentProcessing(context.BotId, context.PayloadPrefix);

        try
        {
            var registry = services.GetRequiredService<PaymentHandlerRegistry>();
            var processors = registry.GetProcessors(context.PayloadPrefix, services);

            foreach (var processor in processors) await processor.ProcessAsync(context, ct).ConfigureAwait(false);

            metrics.PaymentSucceeded(context.BotId, context.PayloadPrefix, (int)context.TotalAmount,
                PaymentCurrency.Xtr);
            logger.LogInformation(
                "Payment processed: user {UserId}, prefix {Prefix}, charge {ChargeId}, amount {Amount}",
                context.UserId, context.PayloadPrefix, context.ChargeId, context.TotalAmount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Payment processing failed: user {UserId}, prefix {Prefix}, charge {ChargeId}",
                context.UserId, context.PayloadPrefix, context.ChargeId);

            metrics.PaymentFailed(context.BotId, context.PayloadPrefix, ex.GetType().Name);

            if (_options.AutoRefundOnFailure) await TryAutoRefundAsync(context, ct).ConfigureAwait(false);

            throw;
        }
    }

    private async Task TryAutoRefundAsync(SuccessfulPaymentContext context, CancellationToken ct)
    {
        try
        {
            var client = botProvider.GetClient(context.BotId);
            await client.RefundStarPayment(context.UserId, context.ChargeId, ct).ConfigureAwait(false);

            logger.LogWarning(
                "Auto-refunded payment: user {UserId}, charge {ChargeId}, amount {Amount}",
                context.UserId, context.ChargeId, context.TotalAmount);

            metrics.RefundProcessed(context.BotId, RefundReason.PaymentFailed, (int)context.TotalAmount);
        }
        catch (Exception refundEx)
        {
            logger.LogCritical(refundEx,
                "CRITICAL: Auto-refund FAILED for user {UserId}, charge {ChargeId}. Manual intervention needed.",
                context.UserId, context.ChargeId);
        }
    }
}
