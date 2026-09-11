using GiftCommerceBot.Data;
using GiftCommerceBot.Data.Entities;
using Microsoft.EntityFrameworkCore;
using TeleForge.Payments.Abstractions;

namespace GiftCommerceBot.Stores;

/// <summary>
/// EF Core implementation of <see cref="ICommerceStore"/>.
/// Persists purchase orders and settlement records in SQLite.
/// </summary>
public class EfCommerceStore(GiftShopDbContext db) : ICommerceStore
{
    public async Task SaveOrderAsync(PurchaseOrder order, CancellationToken ct = default)
    {
        db.PurchaseOrders.Add(Map(order));
        await db.SaveChangesAsync(ct);
    }

    public async Task<PurchaseOrder?> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        var entity = await db.PurchaseOrders.FindAsync([orderId], ct);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<PurchaseOrder>> GetPendingOrdersAsync(long userId,
        CancellationToken ct = default)
    {
        return await db.PurchaseOrders
            .Where(o => o.UserId == userId && o.Status == "Pending")
            .Select(o => Map(o))
            .ToListAsync(ct);
    }

    public async Task UpdateOrderStatusAsync(string orderId, SettlementStatus status,
        string? settlementId, CancellationToken ct = default)
    {
        var entity = await db.PurchaseOrders.FindAsync([orderId], ct);
        if (entity is null) return;

        entity.Status = status.ToString();
        entity.SettlementId = settlementId;
        if (status == SettlementStatus.Confirmed)
            entity.PaidAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task SaveSettlementAsync(SettlementRecord settlement, CancellationToken ct = default)
    {
        db.SettlementRecords.Add(new GiftSettlementRecord
        {
            SettlementId = settlement.SettlementId,
            PurchaseOrderId = settlement.PurchaseOrderId,
            Rail = settlement.Rail.ToString(),
            UserId = settlement.UserId,
            Amount = settlement.Amount,
            Currency = settlement.Currency.ToString(),
            RawData = settlement.RawData
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<SettlementRecord?> GetSettlementAsync(string settlementId, CancellationToken ct = default)
    {
        var entity = await db.SettlementRecords.FindAsync([settlementId], ct);
        if (entity is null) return null;

        return new SettlementRecord
        {
            SettlementId = entity.SettlementId,
            PurchaseOrderId = entity.PurchaseOrderId,
            Rail = Enum.Parse<PurchaseRail>(entity.Rail),
            UserId = entity.UserId,
            Amount = entity.Amount,
            Currency = Enum.Parse<PaymentCurrency>(entity.Currency),
            RawData = entity.RawData
        };
    }

    public async Task<bool> IsSettledAsync(string settlementId, CancellationToken ct = default)
    {
        return await db.SettlementRecords.AnyAsync(s => s.SettlementId == settlementId, ct);
    }

    private static GiftPurchaseOrder Map(PurchaseOrder o)
    {
        return new GiftPurchaseOrder
        {
            Id = o.Id,
            UserId = o.UserId,
            ChatId = o.ChatId,
            BotId = o.BotId,
            Payload = o.Payload,
            Rail = o.Rail.ToString(),
            Currency = o.Currency.ToString(),
            Amount = o.Amount,
            Status = o.Status.ToString(),
            SettlementId = o.SettlementId,
            CreatedAt = o.CreatedAt,
            ExpiresAt = o.ExpiresAt
        };
    }

    private static PurchaseOrder Map(GiftPurchaseOrder o)
    {
        return new PurchaseOrder
        {
            Id = o.Id,
            UserId = o.UserId,
            ChatId = o.ChatId,
            BotId = o.BotId,
            Payload = o.Payload,
            Rail = Enum.Parse<PurchaseRail>(o.Rail),
            Currency = Enum.Parse<PaymentCurrency>(o.Currency),
            Amount = o.Amount,
            Status = Enum.Parse<SettlementStatus>(o.Status),
            SettlementId = o.SettlementId,
            CreatedAt = o.CreatedAt,
            ExpiresAt = o.ExpiresAt
        };
    }
}

/// <summary>
/// EF Core implementation of <see cref="IGiftIntakeStore"/>.
/// Ensures each received gift ID is only processed once.
/// </summary>
public class EfGiftIntakeStore(GiftShopDbContext db) : IGiftIntakeStore
{
    public async Task SaveIntakeAsync(GiftIntakeRecord record, CancellationToken ct = default)
    {
        db.GiftIntakes.Add(new GiftIntakeEntry
        {
            Id = record.Id,
            OwnedGiftId = record.OwnedGiftId,
            GiftId = record.GiftId,
            GiftType = record.GiftType.ToString(),
            SenderUserId = record.SenderUserId,
            ConvertStarCount = record.ConvertStarCount,
            Status = record.Status.ToString()
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<GiftIntakeRecord?> GetByOwnedGiftIdAsync(string ownedGiftId,
        CancellationToken ct = default)
    {
        var entity = await db.GiftIntakes.FirstOrDefaultAsync(g => g.OwnedGiftId == ownedGiftId, ct);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<GiftIntakeRecord>> GetPendingIntakesAsync(int limit = 50,
        CancellationToken ct = default)
    {
        return await db.GiftIntakes
            .Where(g => g.Status == "Pending")
            .Take(limit)
            .Select(g => Map(g))
            .ToListAsync(ct);
    }

    public async Task UpdateIntakeStatusAsync(string id, GiftIntakeStatus status,
        string? purchaseOrderId, CancellationToken ct = default)
    {
        var entity = await db.GiftIntakes.FindAsync([id], ct);
        if (entity is null) return;

        entity.Status = status.ToString();
        entity.PurchaseOrderId = purchaseOrderId;
        await db.SaveChangesAsync(ct);
    }

    private static GiftIntakeRecord Map(GiftIntakeEntry e)
    {
        return new GiftIntakeRecord
        {
            Id = e.Id,
            OwnedGiftId = e.OwnedGiftId,
            GiftId = e.GiftId,
            GiftType = Enum.Parse<GiftType>(e.GiftType),
            SenderUserId = e.SenderUserId,
            ConvertStarCount = e.ConvertStarCount,
            Status = Enum.Parse<GiftIntakeStatus>(e.Status),
            PurchaseOrderId = e.PurchaseOrderId,
            RecordedAt = e.RecordedAt
        };
    }
}
