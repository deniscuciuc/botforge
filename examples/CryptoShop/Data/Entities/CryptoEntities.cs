namespace CryptoShop.Data.Entities;

/// <summary>
/// A product available for purchase with Stars or TON.
/// </summary>
public class CryptoProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Price in Telegram Stars (XTR).</summary>
    public int PriceStars { get; set; }

    /// <summary>Price in nanotons (1 TON = 1_000_000_000 nanotons).</summary>
    public long PriceTon { get; set; }

    /// <summary>Price in USDT micro-units (1 USDT = 1_000_000).</summary>
    public long PriceUsdt { get; set; }

    public bool IsAvailable { get; set; } = true;
}

/// <summary>
/// EF Core entity mirroring the <see cref="BotForge.Payments.Abstractions.WalletPaymentSession"/>.
/// </summary>
public class WalletSessionEntity
{
    public string SessionId { get; set; } = string.Empty;
    public string PurchaseOrderId { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string Rail { get; set; } = string.Empty;
    public long ExpectedAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? DestinationAddress { get; set; }
    public string? TransferMemo { get; set; }
    public string Status { get; set; } = "Pending";
    public string? TransactionHash { get; set; }
    public string? SenderAddress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// EF Core entity mirroring <see cref="BotForge.Payments.Abstractions.PurchaseOrder"/>.
/// </summary>
public class CryptoPurchaseOrder
{
    public string Id { get; set; } = string.Empty;
    public long UserId { get; set; }
    public long ChatId { get; set; }
    public string BotId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Rail { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? SettlementId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? SettledAt { get; set; }
}

/// <summary>
/// EF Core entity mirroring <see cref="BotForge.Payments.Abstractions.SettlementRecord"/>.
/// </summary>
public class CryptoSettlementRecord
{
    public string SettlementId { get; set; } = string.Empty;
    public string PurchaseOrderId { get; set; } = string.Empty;
    public string Rail { get; set; } = string.Empty;
    public long UserId { get; set; }
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = "Confirmed";
    public DateTimeOffset RecordedAt { get; set; }
    public string? RawData { get; set; }
}
