using BotForge.Payments.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BotForge.Payments.Revenue;

public class RevenueService(
    IStarTransactionService transactionService,
    IServiceProvider services,
    ILogger<RevenueService> logger)
    : IRevenueService
{
    public Task<StarBalance> GetStarBalanceAsync(string botId, CancellationToken ct = default)
    {
        return transactionService.GetBalanceAsync(botId, ct);
    }

    public Task<IReadOnlyList<StarTransaction>> GetTransactionsAsync(string botId, int offset = 0, int limit = 100,
        CancellationToken ct = default)
    {
        return transactionService.GetTransactionsAsync(botId, offset, limit, ct);
    }

    public async Task SyncTransactionsAsync(string botId, CancellationToken ct = default)
    {
        var store = services.GetService<IPaymentStore>();
        if (store is null)
        {
            logger.LogDebug("No IPaymentStore registered, skipping transaction sync");
            return;
        }

        var lastSync = await store.GetLastTransactionSyncDateAsync(botId, ct).ConfigureAwait(false);
        var transactions = await transactionService.GetTransactionsAsync(botId, 0, 100, ct).ConfigureAwait(false);

        var records = (from txn in transactions
                       where !lastSync.HasValue || txn.Date > lastSync.Value
                       select new StarTransactionRecord
                       {
                           TransactionId = txn.Id,
                           BotId = botId,
                           Amount = txn.Amount,
                           Direction = txn.Direction,
                           Date = txn.Date,
                           PartnerType = txn.Partner?.Type,
                           PartnerUserId = txn.Partner?.UserId,
                           InvoicePayload = txn.Partner?.InvoicePayload
                       }).ToList();

        if (records.Count > 0)
        {
            await store.SaveStarTransactionsAsync(records, ct).ConfigureAwait(false);
            logger.LogInformation("Synced {Count} new transactions for bot {BotId}", records.Count, botId);
        }
    }

    public async Task<RevenueSnapshot> GetSnapshotAsync(string botId, CancellationToken ct = default)
    {
        var balance = await GetStarBalanceAsync(botId, ct).ConfigureAwait(false);
        var transactions = await GetTransactionsAsync(botId, 0, 100, ct).ConfigureAwait(false);

        return new RevenueSnapshot
        {
            Balance = balance,
            TotalIncomingTransactions = transactions.Count(t => t.Direction == TransactionDirection.Incoming),
            TotalOutgoingTransactions = transactions.Count(t => t.Direction == TransactionDirection.Outgoing)
        };
    }
}
