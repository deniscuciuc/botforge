namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Processes a Telegram-initiated refund for a specific payload type.
/// </summary>
public interface IRefundProcessor
{
    Task<RefundDecision> ProcessAsync(RefundContext context, CancellationToken ct = default);
}
