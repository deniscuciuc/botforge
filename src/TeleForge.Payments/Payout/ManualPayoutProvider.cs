using TeleForge.Payments.Abstractions;

namespace TeleForge.Payments.Payout;

/// <summary>
/// Payout provider that marks the request for manual admin review.
/// Used for currencies/providers that don't support automated processing.
/// </summary>
public class ManualPayoutProvider : IPayoutProvider
{
    public string ProviderName => "Manual";

    public Task<PayoutResult> ExecuteAsync(PayoutRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(PayoutResult.NeedsReview(PayoutProvider.Manual));
    }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }
}
