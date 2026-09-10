namespace BotForge.Payments.Abstractions;

/// <summary>
/// Lifecycle state of a wallet-based or gift-based settlement.
/// </summary>
public enum SettlementStatus
{
    /// <summary>Payment session created, awaiting user action.</summary>
    Pending,

    /// <summary>User signed or transferred; awaiting on-chain / off-chain confirmation.</summary>
    Processing,

    /// <summary>Settlement confirmed and accepted.</summary>
    Confirmed,

    /// <summary>Settlement rejected, failed, or amount/asset mismatch.</summary>
    Failed,

    /// <summary>Payment session expired before user completed payment.</summary>
    Expired,

    /// <summary>Confirmed settlement has been reversed/refunded.</summary>
    Refunded
}
