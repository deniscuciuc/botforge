namespace GiftCommerceBot.Data.Entities;

public class GiftProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int PriceStars { get; set; }

    /// <summary>
    /// Optional comma-separated list of Gift IDs that the bot will accept as payment
    /// for this product (empty = accept any gift whose convert-star count >= PriceStars).
    /// </summary>
    public string GiftIds { get; set; } = "";

    public bool IsAvailable { get; set; } = true;
}

public class GiftPurchaseOrder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public long UserId { get; set; }
    public long ChatId { get; set; }
    public string BotId { get; set; } = "main";
    public int ProductId { get; set; }
    public string Payload { get; set; } = "";
    public string Rail { get; set; } = "";
    public string Currency { get; set; } = "Xtr";
    public long Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public string? SettlementId { get; set; }
    public string? TelegramChargeId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}

public class GiftSettlementRecord
{
    public string SettlementId { get; set; } = "";
    public string PurchaseOrderId { get; set; } = "";
    public string Rail { get; set; } = "";
    public long UserId { get; set; }
    public long Amount { get; set; }
    public string Currency { get; set; } = "";
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? RawData { get; set; }
}

public class GiftIntakeEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string OwnedGiftId { get; set; } = "";
    public string GiftId { get; set; } = "";
    public string GiftType { get; set; } = "";
    public long? SenderUserId { get; set; }
    public int? ConvertStarCount { get; set; }
    public string Status { get; set; } = "Pending";
    public string? PurchaseOrderId { get; set; }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}
