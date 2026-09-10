using BotForge.Payments.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotForge.Payments.Payout;

/// <summary>
/// Background worker that processes queued payouts from <see cref="IPaymentStore"/>.
/// </summary>
public class PayoutWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<PaymentOptions> options,
    ILogger<PayoutWorker> logger)
    : BackgroundService
{
    private readonly PaymentOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Payout worker started, interval: {Interval}, batch size: {BatchSize}",
            _options.PayoutWorkerInterval, _options.PayoutBatchSize);

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                await Task.Delay(_options.PayoutWorkerInterval, stoppingToken).ConfigureAwait(false);
                await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payout worker batch failed");
            }

        logger.LogInformation("Payout worker stopped");
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetService<IPaymentStore>();
        if (store is null) return;

        var pendingPayouts = await store.GetPendingPayoutsAsync(_options.PayoutBatchSize, ct).ConfigureAwait(false);
        if (pendingPayouts.Count == 0) return;

        logger.LogDebug("Processing {Count} pending payouts", pendingPayouts.Count);

        var providers = scope.ServiceProvider.GetServices<IPayoutProvider>().ToList();
        var metrics = scope.ServiceProvider.GetService<IPaymentMetrics>() ?? NullPaymentMetrics.Instance;

        foreach (var payout in pendingPayouts)
        {
            var provider = providers.FirstOrDefault(p =>
                string.Equals(p.ProviderName, payout.Provider.ToString(), StringComparison.OrdinalIgnoreCase));

            if (provider is null)
            {
                logger.LogError("No provider for payout {PayoutId}, provider {Provider}", payout.Id, payout.Provider);
                await store.UpdatePayoutStatusAsync(payout.Id, PayoutStatus.Failed, null, "No provider", ct).ConfigureAwait(false);
                continue;
            }

            if (payout.RetryCount >= _options.PayoutMaxRetries)
            {
                logger.LogWarning("Payout {PayoutId} exceeded max retries, moving to manual review", payout.Id);
                await store.UpdatePayoutStatusAsync(payout.Id, PayoutStatus.ManualReview, null, "Max retries exceeded",
                    ct).ConfigureAwait(false);
                metrics.PayoutCompleted(payout.Provider, payout.Currency, PayoutStatus.ManualReview);
                continue;
            }

            try
            {
                var request = new PayoutRequest
                {
                    UserId = payout.UserId,
                    Amount = payout.Amount,
                    Currency = payout.Currency,
                    Destination = payout.Destination,
                    Provider = payout.Provider,
                    Metadata = payout.Metadata
                };

                var result = await provider.ExecuteAsync(request, ct).ConfigureAwait(false);
                await store.UpdatePayoutStatusAsync(payout.Id, result.Status, result.TransactionId, result.Error, ct).ConfigureAwait(false);
                metrics.PayoutCompleted(payout.Provider, payout.Currency, result.Status);

                logger.LogInformation(
                    "Payout {PayoutId} processed: status {Status}, txn {TransactionId}",
                    payout.Id, result.Status, result.TransactionId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payout {PayoutId} failed, retry {Retry}/{MaxRetry}",
                    payout.Id, payout.RetryCount + 1, _options.PayoutMaxRetries);

                await store.UpdatePayoutStatusAsync(payout.Id, PayoutStatus.Pending, null, ex.Message, ct).ConfigureAwait(false);
            }
        }
    }
}
