namespace BotForge.Payments.Abstractions;

/// <summary>
/// Provider-specific payout execution. Implementations execute
/// the actual transfer (e.g., Fragment, manual, crypto wallet).
/// </summary>
public interface IPayoutProvider
{
    string ProviderName { get; }
    Task<PayoutResult> ExecuteAsync(PayoutRequest request, CancellationToken ct = default);
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}
