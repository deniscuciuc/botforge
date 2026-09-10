namespace BotForge.Payments.Abstractions;

/// <summary>
/// Validates a pre-checkout query for a specific payload type.
/// </summary>
public interface IPreCheckoutValidator
{
    Task<CheckoutValidationResult> ValidateAsync(PreCheckoutContext context, CancellationToken ct = default);
}
