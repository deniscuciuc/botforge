namespace BotForge.Payments.Abstractions;

/// <summary>
/// Orchestrates the payout pipeline: anti-fraud → provider selection → execution → recording.
/// </summary>
public interface IPayoutPipeline
{
    Task<PayoutResult> RequestPayoutAsync(PayoutRequest request, CancellationToken ct = default);
}
