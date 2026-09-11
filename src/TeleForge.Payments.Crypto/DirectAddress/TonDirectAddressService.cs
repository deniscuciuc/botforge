using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeleForge.Payments.Abstractions;
using TeleForge.Payments.Crypto.Options;

namespace TeleForge.Payments.Crypto.DirectAddress;

/// <summary>
/// Manages direct-address TON payment sessions.
/// The bot has a single deposit address; the user must include the session memo/comment
/// in their TON transfer so the observer can match it.
/// </summary>
public class TonDirectAddressService(
    IWalletPaymentService walletPaymentService,
    IOptions<TonPaymentOptions> options,
    ILogger<TonDirectAddressService> logger)
{
    private readonly TonPaymentOptions _options = options.Value;

    /// <summary>
    /// Creates a wallet payment session for the direct-address rail and returns the
    /// session along with payment instructions for the user.
    /// </summary>
    public async Task<DirectAddressPaymentInfo> CreatePaymentAsync(WalletPaymentRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(_options.DepositAddress))
            throw new InvalidOperationException(
                "TonPaymentOptions.DepositAddress must be set to use the TonDirect rail.");

        var directRequest = new WalletPaymentRequest
        {
            PurchaseOrderId = request.PurchaseOrderId,
            UserId = request.UserId,
            Rail = PurchaseRail.TonDirect,
            Amount = request.Amount,
            Currency = request.Currency,
            Expiry = request.Expiry,
            UserWalletAddress = request.UserWalletAddress
        };
        var result = await walletPaymentService.CreateSessionAsync(directRequest, ct).ConfigureAwait(false);

        if (!result.Success || result.Session is null)
            throw new InvalidOperationException($"Could not create wallet session: {result.Error}");

        var session = result.Session;

        logger.LogInformation(
            "Direct-address TON payment session {SessionId} created — deposit {Amount} nanotons to {Address} with comment «{Memo}»",
            session.SessionId, request.Amount, _options.DepositAddress, session.TransferMemo);

        return new DirectAddressPaymentInfo(
            session,
            _options.DepositAddress,
            session.TransferMemo!,
            _options.UsdtJettonWalletAddress);
    }
}

/// <summary>
/// Payment instructions returned to the bot flow for display to the user.
/// </summary>
public record DirectAddressPaymentInfo(
    WalletPaymentSession Session,
    string DepositAddress,
    string RequiredMemo,
    string? UsdtJettonAddress);
