namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Request to initiate a wallet-based payment session.
/// </summary>
public class WalletPaymentRequest
{
    /// <summary>Purchase order to settle with this wallet payment.</summary>
    public required string PurchaseOrderId { get; init; }

    /// <summary>Telegram user who is paying.</summary>
    public required long UserId { get; init; }

    /// <summary>Payment rail to use.</summary>
    public required PurchaseRail Rail { get; init; }

    /// <summary>Amount expected in the smallest unit of <see cref="Currency"/>.</summary>
    public required long Amount { get; init; }

    /// <summary>Asset to accept.</summary>
    public required PaymentCurrency Currency { get; init; }

    /// <summary>How long the session should remain valid (default: 30 minutes).</summary>
    public TimeSpan Expiry { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>Optional user-supplied wallet address (for pre-filling TON Connect).</summary>
    public string? UserWalletAddress { get; init; }
}

/// <summary>
/// Result of initiating a wallet payment session.
/// </summary>
public class WalletPaymentResult
{
    public bool Success { get; private init; }
    public string? Error { get; private init; }
    public WalletPaymentSession? Session { get; private init; }

    /// <summary>
    /// URL or deep-link to open in the Mini App / Web App wallet UI.
    /// Null if no redirect is needed (e.g. direct-address flows the bot handles inline).
    /// </summary>
    public string? PaymentUrl { get; private init; }

    public static WalletPaymentResult Ok(WalletPaymentSession session, string? paymentUrl = null)
    {
        return new WalletPaymentResult { Success = true, Session = session, PaymentUrl = paymentUrl };
    }

    public static WalletPaymentResult Fail(string error)
    {
        return new WalletPaymentResult { Success = false, Error = error };
    }
}
