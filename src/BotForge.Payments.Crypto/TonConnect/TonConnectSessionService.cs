using System.Globalization;
using BotForge.Payments.Abstractions;
using BotForge.Payments.Crypto.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotForge.Payments.Crypto.TonConnect;

/// <summary>
/// Manages TON Connect payment sessions for the Mini App / Web App flow.
/// On the server side this creates the wallet payment session and builds the
/// deep-link / universal URL that the Mini App opens to prompt the user's wallet.
/// The actual ECDH connection handshake happens client-side in the Mini App.
/// </summary>
public class TonConnectSessionService(
    IWalletPaymentService walletPaymentService,
    IOptions<TonPaymentOptions> options,
    ILogger<TonConnectSessionService> logger)
{
    private readonly TonPaymentOptions _options = options.Value;

    /// <summary>
    /// Creates a session for the TON Connect rail and returns the deep-link URL
    /// the Mini App should open to trigger the wallet.
    /// </summary>
    public async Task<TonConnectPaymentInfo> CreateSessionAsync(WalletPaymentRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(_options.DepositAddress))
            throw new InvalidOperationException(
                "TonPaymentOptions.DepositAddress must be set to use the TonConnect rail.");

        var connectRequest = new WalletPaymentRequest
        {
            PurchaseOrderId = request.PurchaseOrderId,
            UserId = request.UserId,
            Rail = PurchaseRail.TonConnect,
            Amount = request.Amount,
            Currency = request.Currency,
            Expiry = request.Expiry,
            UserWalletAddress = request.UserWalletAddress
        };
        var result = await walletPaymentService.CreateSessionAsync(connectRequest, ct).ConfigureAwait(false);

        if (!result.Success || result.Session is null)
            throw new InvalidOperationException($"Could not create wallet session: {result.Error}");

        var session = result.Session;
        var deepLink = BuildDeepLink(session.TransferMemo!, session.ExpectedAmount);

        logger.LogInformation(
            "TON Connect session {SessionId} created with deep-link",
            session.SessionId);

        return new TonConnectPaymentInfo(session, deepLink);
    }

    private string BuildDeepLink(string memo, long amountNano)
    {
        // If a custom template is provided, use it; otherwise fall back to the
        // standard Tonkeeper universal link format.
        if (!string.IsNullOrEmpty(_options.TonConnectDeepLinkTemplate))
            return _options.TonConnectDeepLinkTemplate
                .Replace("{memo}", Uri.EscapeDataString(memo))
                .Replace("{amount}", amountNano.ToString(CultureInfo.InvariantCulture))
                .Replace("{address}", Uri.EscapeDataString(_options.DepositAddress!));

        // Standard Tonkeeper / TON Space link:
        // https://app.tonkeeper.com/transfer/{address}?amount={nanotons}&text={memo}
        return
            $"https://app.tonkeeper.com/transfer/{Uri.EscapeDataString(_options.DepositAddress!)}" +
            $"?amount={amountNano}" +
            $"&text={Uri.EscapeDataString(memo)}";
    }
}

/// <summary>
/// Result of creating a TON Connect session.
/// </summary>
public record TonConnectPaymentInfo(
    WalletPaymentSession Session,
    string PaymentUrl);
