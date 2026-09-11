namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Revenue queries: bot Star balance and transaction-based revenue calculation.
/// </summary>
public interface IRevenueService
{
    Task<StarBalance> GetStarBalanceAsync(string botId, CancellationToken ct = default);

    Task<IReadOnlyList<StarTransaction>> GetTransactionsAsync(string botId, int offset = 0, int limit = 100,
        CancellationToken ct = default);

    Task SyncTransactionsAsync(string botId, CancellationToken ct = default);
    Task<RevenueSnapshot> GetSnapshotAsync(string botId, CancellationToken ct = default);
}
