using Microsoft.Extensions.Logging;
using TeleForge.Payments.Abstractions;
using TeleForge.Payments.Fragment.Api;

namespace TeleForge.Payments.Fragment;

/// <summary>
/// <see cref="IPayoutProvider"/> implementation that executes payouts
/// via the Fragment API (buy stars for target user).
/// </summary>
internal sealed class FragmentPayoutProvider(
    FragmentClient client,
    IPaymentMetrics metrics,
    ILogger<FragmentPayoutProvider> logger)
    : IPayoutProvider
{
    public string ProviderName => "Fragment";

    public async Task<PayoutResult> ExecuteAsync(PayoutRequest request, CancellationToken ct = default)
    {
        metrics.PayoutRequested(PayoutProvider.Fragment, request.Currency);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await client.SendStarsAsync(
                request.Destination,
                request.Amount,
                ct: ct).ConfigureAwait(false);

            sw.Stop();
            metrics.PayoutDuration(PayoutProvider.Fragment, sw.Elapsed.TotalMilliseconds);

            if (response is null)
            {
                var result = PayoutResult.Failed("Empty response from Fragment API", PayoutProvider.Fragment);
                metrics.PayoutCompleted(PayoutProvider.Fragment, request.Currency, result.Status);
                return result;
            }

            if (response.Status is "completed" or "processing")
            {
                var result = response.Status == "completed"
                    ? PayoutResult.Ok(response.Id, PayoutProvider.Fragment)
                    : PayoutResult.Queued(PayoutProvider.Fragment);
                metrics.PayoutCompleted(PayoutProvider.Fragment, request.Currency, result.Status);
                logger.LogInformation(
                    "Fragment payout {OrderId} for user {Destination}: {Status}, amount {Amount}",
                    response.Id, request.Destination, response.Status, request.Amount);
                return result;
            }

            var failResult = PayoutResult.Failed(
                $"Fragment order {response.Id} status: {response.Status}", PayoutProvider.Fragment);
            metrics.PayoutCompleted(PayoutProvider.Fragment, request.Currency, failResult.Status);
            logger.LogWarning(
                "Fragment payout {OrderId} unexpected status: {Status}", response.Id, response.Status);
            return failResult;
        }
        catch (FragmentApiException ex)
        {
            sw.Stop();
            metrics.PayoutDuration(PayoutProvider.Fragment, sw.Elapsed.TotalMilliseconds);
            var result = PayoutResult.Failed($"Fragment API error: {ex.Message}", PayoutProvider.Fragment);
            metrics.PayoutCompleted(PayoutProvider.Fragment, request.Currency, result.Status);
            logger.LogError(ex, "Fragment payout failed for {Destination}, amount {Amount}",
                request.Destination, request.Amount);
            return result;
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var wallet = await client.GetWalletBalanceAsync(ct).ConfigureAwait(false);
            return wallet is not null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fragment availability check failed");
            return false;
        }
    }
}
