using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeleForge.Payments.Abstractions;
using TeleForge.Payments.Crypto.Options;
using TeleForge.Payments.Crypto.Rpc;

namespace TeleForge.Payments.Crypto.Observer;

/// <summary>
/// Background worker that periodically polls the TON blockchain for incoming transfers
/// that match any pending wallet payment sessions.
/// When a matching transfer is found, it calls <see cref="IWalletPaymentService.RecordTransferAsync"/>
/// to confirm the session and trigger fulfillment.
/// </summary>
public class TonBlockchainObserverWorker(
    IWalletPaymentStore walletStore,
    IWalletPaymentService walletPaymentService,
    ITonRpcClient tonRpcClient,
    IOptions<TonPaymentOptions> options,
    ILogger<TonBlockchainObserverWorker> logger)
    : BackgroundService
{
    private readonly TonPaymentOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TON blockchain observer started (polling every {Interval})",
            _options.PollingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during TON blockchain polling cycle");
            }

            await Task.Delay(_options.PollingInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_options.DepositAddress))
            return;

        var pendingSessions = await walletStore.GetPendingSessionsAsync(ct: ct).ConfigureAwait(false);
        if (pendingSessions.Count == 0)
            return;

        // Build a fast lookup by memo → session
        var memoIndex = pendingSessions
            .Where(s => !string.IsNullOrEmpty(s.TransferMemo))
            .ToDictionary(s => s.TransferMemo!, s => s);

        if (memoIndex.Count == 0)
            return;

        var transactions = await tonRpcClient.GetTransactionsAsync(
            _options.DepositAddress,
            _options.MaxTransactionsPerPoll,
            ct).ConfigureAwait(false);

        foreach (var tx in transactions)
        {
            if (string.IsNullOrEmpty(tx.Comment) || !memoIndex.ContainsKey(tx.Comment))
                continue;

            var currency = tx.IsJetton ? PaymentCurrency.Usdt : PaymentCurrency.Ton;
            var session = memoIndex[tx.Comment];

            logger.LogInformation(
                "TON observer matched tx {Hash} to session {SessionId} (memo: {Memo})",
                tx.Hash, session.SessionId, tx.Comment);

            await walletPaymentService.RecordTransferAsync(
                tx.Comment,
                tx.Hash,
                tx.FromAddress,
                tx.Amount,
                currency,
                ct).ConfigureAwait(false);

            // Remove from index to avoid double-processing in the same poll cycle
            memoIndex.Remove(tx.Comment);
        }
    }
}
