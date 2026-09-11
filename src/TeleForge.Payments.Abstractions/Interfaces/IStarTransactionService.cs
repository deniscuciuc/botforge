namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Queries bot's Star transactions and balance from the Telegram API.
/// </summary>
public interface IStarTransactionService
{
    Task<StarBalance> GetBalanceAsync(string botId, CancellationToken ct = default);

    Task<IReadOnlyList<StarTransaction>> GetTransactionsAsync(string botId, int offset = 0, int limit = 100,
        CancellationToken ct = default);
}
