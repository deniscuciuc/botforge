namespace BotForge.Payments.Abstractions;

/// <summary>
/// Processes a successful payment for a specific payload type.
/// </summary>
public interface IPaymentProcessor
{
    Task ProcessAsync(SuccessfulPaymentContext context, CancellationToken ct = default);
}
