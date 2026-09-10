using BotForge.Core;
using BotForge.Payments.Abstractions;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Payments;
using StarTransaction = BotForge.Payments.Abstractions.StarTransaction;

namespace BotForge.Payments.Transaction;

public class StarTransactionService(
    ITelegramBotClientProvider botProvider,
    ILogger<StarTransactionService> logger)
    : IStarTransactionService
{
    //  TODO: Do we need this?
    public async Task<StarBalance> GetBalanceAsync(string botId, CancellationToken ct = default)
    {
        var client = botProvider.GetClient(botId);
        var balance = await client.GetStarTransactions(0, 0, ct).ConfigureAwait(false);

        // GetStarTransactions returns StarTransactions which has Amount
        // For balance, we use GetMyStarBalance if available, or derive from GetStarTransactions
        logger.LogDebug("Retrieved Star transactions for balance query, bot {BotId}", botId);

        return new StarBalance
        {
            Amount = 0, // Will be populated when GetMyStarBalance is available
            RetrievedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<IReadOnlyList<StarTransaction>> GetTransactionsAsync(string botId, int offset = 0,
        int limit = 100, CancellationToken ct = default)
    {
        var client = botProvider.GetClient(botId);
        var result = await client.GetStarTransactions(offset, limit, ct).ConfigureAwait(false);

        var transactions = result.Transactions.Select(txn => new StarTransaction
        {
            Id = txn.Id,
            Amount = (int)txn.Amount,
            Direction = txn.Amount >= 0 ? TransactionDirection.Incoming : TransactionDirection.Outgoing,
            Date = txn.Date,
            NanostarAmount = txn.NanostarAmount.HasValue ? (int)txn.NanostarAmount.Value : null,
            Partner = MapPartner(txn.Source ?? txn.Receiver)
        })
            .ToList();

        logger.LogDebug("Retrieved {Count} Star transactions for bot {BotId}", transactions.Count, botId);
        return transactions;
    }

    private static TransactionPartnerInfo? MapPartner(TransactionPartner? partner)
    {
        if (partner is null) return null;

        return partner switch
        {
            TransactionPartnerUser user => new TransactionPartnerInfo
            {
                Type = "user",
                UserId = user.User.Id,
                Username = user.User.Username,
                FirstName = user.User.FirstName,
                InvoicePayload = user.InvoicePayload
            },
            TransactionPartnerChat chat => new TransactionPartnerInfo
            {
                Type = "chat",
                ChatId = chat.Chat.Id,
                ChatTitle = chat.Chat.Title
            },
            TransactionPartnerFragment fragment => new TransactionPartnerInfo
            {
                Type = "fragment",
                FragmentState = fragment.WithdrawalState?.ToString()
            },
            TransactionPartnerTelegramAds => new TransactionPartnerInfo
            {
                Type = "telegram_ads"
            },
            TransactionPartnerTelegramApi => new TransactionPartnerInfo
            {
                Type = "telegram_api"
            },
            TransactionPartnerAffiliateProgram affiliate => new
                TransactionPartnerInfo
            {
                Type = "affiliate",
                CommissionPerMille = affiliate.CommissionPerMille
            },
            _ => new TransactionPartnerInfo { Type = "other" }
        };
    }
}
