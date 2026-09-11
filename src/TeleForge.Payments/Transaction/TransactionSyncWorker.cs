using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeleForge.Core;
using TeleForge.Payments.Abstractions;

namespace TeleForge.Payments.Transaction;

/// <summary>
/// Optional background worker that periodically syncs Star transactions to the local store.
/// </summary>
public class TransactionSyncWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<PaymentOptions> options,
    ILogger<TransactionSyncWorker> logger)
    : BackgroundService
{
    private readonly PaymentOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.RevenueSyncEnabled)
        {
            logger.LogInformation("Transaction sync is disabled");
            return;
        }

        logger.LogInformation("Transaction sync worker started, interval: {Interval}", _options.RevenueSyncInterval);

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                await Task.Delay(_options.RevenueSyncInterval, stoppingToken).ConfigureAwait(false);
                await SyncAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction sync failed");
            }
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var revenue = scope.ServiceProvider.GetService<IRevenueService>();
        if (revenue is null)
        {
            logger.LogWarning("IRevenueService not registered, skipping sync");
            return;
        }

        var botProvider = scope.ServiceProvider.GetService<ITelegramBotClientProvider>();
        if (botProvider is null) return;

        foreach (var botId in botProvider.GetRegisteredBotKeys())
            try
            {
                await revenue.SyncTransactionsAsync(botId, ct).ConfigureAwait(false);
                logger.LogDebug("Synced transactions for bot {BotId}", botId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync transactions for bot {BotId}", botId);
            }
    }
}
