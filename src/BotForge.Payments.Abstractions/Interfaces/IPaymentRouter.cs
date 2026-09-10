namespace BotForge.Payments.Abstractions;

/// <summary>
/// Routes payment updates to the correct handler based on payload prefix.
/// </summary>
public interface IPaymentRouter
{
    Task HandlePreCheckoutAsync(PreCheckoutContext context, CancellationToken ct = default);
    Task HandleSuccessfulPaymentAsync(SuccessfulPaymentContext context, CancellationToken ct = default);
    Task HandleRefundAsync(RefundContext context, CancellationToken ct = default);
}
