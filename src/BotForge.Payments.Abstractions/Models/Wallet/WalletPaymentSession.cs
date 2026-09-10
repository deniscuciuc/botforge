namespace BotForge.Payments.Abstractions;

/// <summary>
/// A pending or active wallet-based payment session (TON Connect, direct-address, or custodial).
/// </summary>
public class WalletPaymentSession
{
    /// <summary>Framework-assigned session id (used as the reference in TON comment fields).</summary>
    public string SessionId { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>The purchase order associated with this session.</summary>
    public required string PurchaseOrderId { get; init; }

    /// <summary>Telegram user who initiated this payment.</summary>
    public required long UserId { get; init; }

    /// <summary>Payment rail.</summary>
    public required PurchaseRail Rail { get; init; }

    /// <summary>Expected amount in nanotons (or smallest jetton unit for USDT).</summary>
    public required long ExpectedAmount { get; init; }

    /// <summary>Asset being transferred.</summary>
    public required PaymentCurrency Currency { get; init; }

    /// <summary>
    /// Destination address for direct-address and some custodial flows.
    /// Null for TON Connect sessions where the destination is embedded in the Mini App.
    /// </summary>
    public string? DestinationAddress { get; init; }

    /// <summary>
    /// Memo / comment required in the transfer so the observer can correlate.
    /// For TON Connect this is embedded in the unsigned payload; for direct-address it is
    /// the transfer comment the user must include.
    /// </summary>
    public string? TransferMemo { get; init; }

    /// <summary>Current session status.</summary>
    public SettlementStatus Status { get; set; } = SettlementStatus.Pending;

    /// <summary>Confirmed transaction hash once the transfer is observed on-chain (or provider reference).</summary>
    public string? TransactionHash { get; set; }

    /// <summary>Address that was confirmed as the sender.</summary>
    public string? SenderAddress { get; set; }

    /// <summary>When this session was created.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When this session expires.</summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Provider-specific metadata (e.g. custodial wallet provider session id).</summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}
