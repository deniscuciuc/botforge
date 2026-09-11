using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeleForge.Payments.Crypto.Options;

namespace TeleForge.Payments.Crypto.Rpc;

/// <summary>
/// Default <see cref="ITonRpcClient"/> implementation backed by the Toncenter HTTP API v2.
/// Replace this registration with your own if you prefer TON API, a testnet proxy, etc.
/// </summary>
public class ToncenterRpcClient(
    IHttpClientFactory httpClientFactory,
    IOptions<TonPaymentOptions> options,
    ILogger<ToncenterRpcClient> logger)
    : ITonRpcClient
{
    private readonly TonPaymentOptions _options = options.Value;

    public async Task<IReadOnlyList<TonTransaction>> GetTransactionsAsync(
        string address, int limit = 50, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient(TonPaymentOptions.HttpClientName);

        var url = $"getTransactions?address={Uri.EscapeDataString(address)}&limit={limit}";
        if (_options.ApiKey is not null)
            url += $"&api_key={Uri.EscapeDataString(_options.ApiKey)}";

        try
        {
            var response = await client.GetFromJsonAsync<ToncenterResponse>(url, ct).ConfigureAwait(false);
            if (response?.Ok != true || response.Result is null)
                return Array.Empty<TonTransaction>();

            return response.Result
                .Where(tx => tx.InMsg is not null)
                .Select(MapTransaction)
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to fetch transactions for address {Address}", address);
            return Array.Empty<TonTransaction>();
        }
    }

    private static TonTransaction MapTransaction(ToncenterTx tx)
    {
        var msg = tx.InMsg!;
        var isJetton = msg.MsgData?.Body?.Contains("jetton") == true;

        // Amount is in nanotons
        var amount = long.TryParse(msg.Value, out var v) ? v : 0L;
        var ts = DateTimeOffset.FromUnixTimeSeconds(tx.Utime);

        return new TonTransaction(
            tx.TransactionId?.Hash ?? string.Empty,
            msg.Source ?? string.Empty,
            amount,
            msg.Message,
            isJetton,
            null,
            ts);
    }

    // --- Minimal deserialization models ---

    private sealed class ToncenterResponse
    {
        [JsonPropertyName("ok")] public bool Ok { get; init; }
        [JsonPropertyName("result")] public List<ToncenterTx>? Result { get; init; }
    }

    private sealed class ToncenterTx
    {
        [JsonPropertyName("utime")] public long Utime { get; init; }
        [JsonPropertyName("transaction_id")] public TxId? TransactionId { get; init; }
        [JsonPropertyName("in_msg")] public TxMsg? InMsg { get; init; }
    }

    private sealed class TxId
    {
        [JsonPropertyName("hash")] public string? Hash { get; init; }
    }

    private sealed class TxMsg
    {
        [JsonPropertyName("source")] public string? Source { get; init; }
        [JsonPropertyName("value")] public string? Value { get; init; }
        [JsonPropertyName("message")] public string? Message { get; init; }
        [JsonPropertyName("msg_data")] public MsgData? MsgData { get; init; }
    }

    private sealed class MsgData
    {
        [JsonPropertyName("body")] public string? Body { get; init; }
    }
}
