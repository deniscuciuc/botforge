namespace TeleForge.Payments.Crypto.Rpc;

/// <summary>
/// Minimal abstraction over the TON HTTP API, enabling both the default Toncenter client and
/// custom implementations (e.g. TON API, TonWeb proxy, testnets).
/// </summary>
public interface ITonRpcClient
{
    /// <summary>
    /// Returns recent transactions for the given account address.
    /// </summary>
    /// <param name="address">Wallet address (raw or user-friendly).</param>
    /// <param name="limit">Maximum number of transactions to return.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<TonTransaction>> GetTransactionsAsync(
        string address, int limit = 50, CancellationToken ct = default);
}

/// <summary>
/// Lightweight DTO for an inbound TON/jetton transfer.
/// </summary>
public record TonTransaction(
    string Hash,
    string FromAddress,
    long Amount,
    string? Comment,
    bool IsJetton,
    string? JettonSymbol,
    DateTimeOffset Timestamp);
