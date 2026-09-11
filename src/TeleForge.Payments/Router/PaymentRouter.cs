using Microsoft.Extensions.Logging;
using TeleForge.Payments.Abstractions;
using TeleForge.Payments.Checkout;
using TeleForge.Payments.Invoice;
using TeleForge.Payments.Refund;
using Telegram.Bot.Types.Payments;

namespace TeleForge.Payments.Router;

/// <summary>
/// Routes payment updates to the correct pipeline based on payload prefix.
/// </summary>
public class PaymentRouter(
    PreCheckoutPipeline checkoutPipeline,
    PaymentProcessingPipeline paymentPipeline,
    RefundPipeline refundPipeline,
    PaymentHandlerRegistry registry,
    ILogger<PaymentRouter> logger)
    : IPaymentRouter
{
    public async Task HandlePreCheckoutAsync(PreCheckoutContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!registry.HasHandlers(context.PayloadPrefix))
        {
            logger.LogWarning("No handlers registered for payment prefix {Prefix}", context.PayloadPrefix);
            return;
        }

        await checkoutPipeline.HandleAsync(context, ct).ConfigureAwait(false);
    }

    public async Task HandleSuccessfulPaymentAsync(SuccessfulPaymentContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!registry.HasHandlers(context.PayloadPrefix))
        {
            logger.LogWarning("No handlers registered for payment prefix {Prefix}", context.PayloadPrefix);
            return;
        }

        await paymentPipeline.HandleAsync(context, ct).ConfigureAwait(false);
    }

    public async Task HandleRefundAsync(RefundContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!registry.HasHandlers(context.PayloadPrefix))
        {
            logger.LogWarning("No handlers registered for refund prefix {Prefix}", context.PayloadPrefix);
            return;
        }

        await refundPipeline.HandleAsync(context, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Build a <see cref="PreCheckoutContext"/> from raw update data.
    /// </summary>
    public static PreCheckoutContext BuildPreCheckoutContext(
        PreCheckoutQuery query, string botId)
    {
        ArgumentNullException.ThrowIfNull(query);

        var payload = query.InvoicePayload;
        var prefix = PayloadSerializer.GetPrefix(payload);

        return new PreCheckoutContext
        {
            Query = query,
            BotId = botId,
            PayloadPrefix = prefix,
            RawPayload = payload
        };
    }

    /// <summary>
    /// Build a <see cref="SuccessfulPaymentContext"/> from raw update data.
    /// </summary>
    public static SuccessfulPaymentContext BuildSuccessfulPaymentContext(
        SuccessfulPayment payment,
        string botId, long chatId, long userId, int messageId)
    {
        ArgumentNullException.ThrowIfNull(payment);

        var payload = payment.InvoicePayload;
        var prefix = PayloadSerializer.GetPrefix(payload);

        return new SuccessfulPaymentContext
        {
            Payment = payment,
            BotId = botId,
            PayloadPrefix = prefix,
            RawPayload = payload,
            ChatId = chatId,
            UserId = userId,
            MessageId = messageId
        };
    }

    /// <summary>
    /// Build a <see cref="RefundContext"/> from raw update data.
    /// </summary>
    public static RefundContext BuildRefundContext(
        RefundedPayment refund,
        string botId, long chatId, long userId)
    {
        ArgumentNullException.ThrowIfNull(refund);

        var payload = refund.InvoicePayload;
        var prefix = PayloadSerializer.GetPrefix(payload);

        return new RefundContext
        {
            Refund = refund,
            BotId = botId,
            PayloadPrefix = prefix,
            RawPayload = payload,
            ChatId = chatId,
            UserId = userId
        };
    }
}
