namespace BotForge.Payments.Crypto.Options;

/// <summary>
/// Configuration for TON-based wallet payment rails.
/// </summary>
public class TonPaymentOptions
{
    public const string SectionName = "TonPayments";

    /// <summary>
    /// TON HTTP API v2 endpoint (e.g. https://toncenter.com/api/v2/).
    /// Used by the default <see cref="Rpc.ToncenterRpcClient"/> implementation.
    /// </summary>
    public string RpcEndpoint { get; set; } = "https://toncenter.com/api/v2/";

    /// <summary>
    /// API key for the TON HTTP API.  Required for production use to avoid rate limits.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Bot/operator deposit wallet address for the direct-address rail.
    /// Users will send TON or jettons to this address with a unique memo comment.
    /// </summary>
    public string? DepositAddress { get; set; }

    /// <summary>
    /// Jetton wallet address for USDT-on-TON deposits (TRC-20 equivalent on TON).
    /// If null, USDT payments via direct-address rail are disabled.
    /// </summary>
    public string? UsdtJettonWalletAddress { get; set; }

    /// <summary>
    /// How often the <see cref="Observer.TonBlockchainObserverWorker"/> polls for new transactions.
    /// Default: 15 seconds.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Maximum number of transactions fetched per polling cycle per address.
    /// </summary>
    public int MaxTransactionsPerPoll { get; set; } = 50;

    /// <summary>
    /// Deep-link template for Mini App TON Connect payment flow.
    /// Use {memo} and {amount} placeholders.
    /// Example: "https://app.tonkeeper.com/transfer/{address}?amount={amount}&text={memo}"
    /// </summary>
    public string? TonConnectDeepLinkTemplate { get; set; }

    /// <summary>
    /// Name of the <see cref="System.Net.Http.HttpClient"/> registered for TON RPC calls.
    /// </summary>
    internal const string HttpClientName = "TonRpc";
}
