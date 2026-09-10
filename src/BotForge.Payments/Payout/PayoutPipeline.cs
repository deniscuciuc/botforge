using System.Diagnostics;
using BotForge.Payments.Abstractions;
using BotForge.Payments.AntiFraud;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BotForge.Payments.Payout;

public class PayoutPipeline(
    AntiFraudPipeline antiFraud,
    IServiceProvider services,
    IPaymentMetrics metrics,
    ILogger<PayoutPipeline> logger)
    : IPayoutPipeline
{
    public async Task<PayoutResult> RequestPayoutAsync(PayoutRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        metrics.PayoutRequested(request.Provider, request.Currency);

        var fraudContext = new AntiFraudContext
        {
            UserId = request.UserId,
            Amount = request.Amount,
            Currency = request.Currency,
            Provider = request.Provider,
            Destination = request.Destination,
            Metadata = request.Metadata,
            Services = services
        };

        var fraudResult = await antiFraud.EvaluateAsync(fraudContext, ct).ConfigureAwait(false);
        if (!fraudResult.Passed)
        {
            logger.LogWarning("Payout rejected by anti-fraud for user {UserId}: {Reason}", request.UserId,
                fraudResult.FailureReason);
            metrics.PayoutCompleted(request.Provider, request.Currency, PayoutStatus.Rejected);
            return PayoutResult.Failed($"Anti-fraud: {fraudResult.FailureReason}", request.Provider);
        }

        var providers = services.GetServices<IPayoutProvider>();
        var provider = providers.FirstOrDefault(p =>
            string.Equals(p.ProviderName, request.Provider.ToString(), StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            logger.LogError("No payout provider found for {Provider}", request.Provider);
            return PayoutResult.Failed($"No payout provider: {request.Provider}", request.Provider);
        }

        var store = services.GetService<IPaymentStore>();
        string? payoutId = null;
        if (store != null)
            payoutId = await store.SavePayoutAsync(new PayoutRecord
            {
                UserId = request.UserId,
                Amount = request.Amount,
                Currency = request.Currency,
                Destination = request.Destination,
                Provider = request.Provider,
                Status = PayoutStatus.Processing,
                Metadata = request.Metadata
            }, ct).ConfigureAwait(false);

        var startTime = Stopwatch.GetTimestamp();
        try
        {
            var result = await provider.ExecuteAsync(request, ct).ConfigureAwait(false);
            var elapsed = Stopwatch.GetElapsedTime(startTime);
            metrics.PayoutDuration(request.Provider, elapsed.TotalMilliseconds);
            metrics.PayoutCompleted(request.Provider, request.Currency, result.Status);

            if (store != null && payoutId != null)
                await store.UpdatePayoutStatusAsync(payoutId, result.Status, result.TransactionId, result.Error, ct).ConfigureAwait(false);

            logger.LogInformation(
                "Payout result: user {UserId}, provider {Provider}, status {Status}, txn {TransactionId}",
                request.UserId, request.Provider, result.Status, result.TransactionId);

            return result;
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTime);
            metrics.PayoutDuration(request.Provider, elapsed.TotalMilliseconds);
            metrics.PayoutCompleted(request.Provider, request.Currency, PayoutStatus.Failed);

            if (store != null && payoutId != null)
                await store.UpdatePayoutStatusAsync(payoutId, PayoutStatus.Failed, null, ex.Message, ct).ConfigureAwait(false);

            logger.LogError(ex, "Payout failed for user {UserId}, provider {Provider}", request.UserId,
                request.Provider);
            return PayoutResult.Failed(ex.Message, request.Provider);
        }
    }
}
