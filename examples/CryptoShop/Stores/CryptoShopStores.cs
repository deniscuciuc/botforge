using CryptoShop.Data;
using CryptoShop.Data.Entities;
using Microsoft.EntityFrameworkCore;
using TeleForge.Payments.Abstractions;

namespace CryptoShop.Stores;

/// <summary>
/// EF Core implementation of <see cref="ICommerceStore"/> for the CryptoShop example.
/// Stores PurchaseOrders and SettlementRecords in SQLite.
/// </summary>
public class EfCommerceStore(CryptoShopDbContext db) : ICommerceStore
{
    public async Task SaveOrderAsync(PurchaseOrder order, CancellationToken ct = default)
    {
        var existing = await db.PurchaseOrders.FindAsync([order.Id], ct);
        if (existing is not null)
            return;

        db.PurchaseOrders.Add(ToEntity(order));
        await db.SaveChangesAsync(ct);
    }

    public async Task<PurchaseOrder?> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        var entity = await db.PurchaseOrders.FindAsync([orderId], ct);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyList<PurchaseOrder>> GetPendingOrdersAsync(long userId, CancellationToken ct = default)
    {
        var entities = await db.PurchaseOrders
            .Where(o => o.UserId == userId && o.Status == "Pending")
            .ToListAsync(ct);
        return entities.Select(ToModel).ToList();
    }

    public async Task UpdateOrderStatusAsync(string orderId, SettlementStatus status, string? settlementId,
        CancellationToken ct = default)
    {
        var entity = await db.PurchaseOrders.FindAsync([orderId], ct);
        if (entity is null) return;

        entity.Status = status.ToString();
        entity.SettlementId = settlementId ?? entity.SettlementId;
        if (status == SettlementStatus.Confirmed)
            entity.SettledAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task SaveSettlementAsync(SettlementRecord settlement, CancellationToken ct = default)
    {
        if (await db.SettlementRecords.AnyAsync(r => r.SettlementId == settlement.SettlementId, ct))
            return;

        db.SettlementRecords.Add(new CryptoSettlementRecord
        {
            SettlementId = settlement.SettlementId,
            PurchaseOrderId = settlement.PurchaseOrderId,
            Rail = settlement.Rail.ToString(),
            UserId = settlement.UserId,
            Amount = settlement.Amount,
            Currency = settlement.Currency.ToString(),
            Status = settlement.Status.ToString(),
            RecordedAt = settlement.RecordedAt,
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
            Status = Enum.Parse<SettlementStatus>(entity.Status),
            RecordedAt = entity.RecordedAt,
            RawData = entity.RawData
        };
    }

    public async Task<bool> IsSettledAsync(string settlementId, CancellationToken ct = default)
    {
        return await db.SettlementRecords.AnyAsync(r => r.SettlementId == settlementId, ct);
    }

    private static CryptoPurchaseOrder ToEntity(PurchaseOrder o)
    {
        return new CryptoPurchaseOrder
        {
            Id = o.Id,
            UserId = o.UserId,
            ChatId = o.ChatId,
            BotId = o.BotId,
            Payload = o.Payload,
            Amount = o.Amount,
            Currency = o.Currency.ToString(),
            Rail = o.Rail.ToString(),
            Status = o.Status.ToString(),
            SettlementId = o.SettlementId,
            CreatedAt = o.CreatedAt,
            ExpiresAt = o.ExpiresAt
        };
    }

    private static PurchaseOrder ToModel(CryptoPurchaseOrder e)
    {
        return new PurchaseOrder
        {
            Id = e.Id,
            UserId = e.UserId,
            ChatId = e.ChatId,
            BotId = e.BotId,
            Payload = e.Payload,
            Amount = e.Amount,
            Currency = Enum.Parse<PaymentCurrency>(e.Currency),
            Rail = Enum.Parse<PurchaseRail>(e.Rail),
            Status = Enum.Parse<SettlementStatus>(e.Status),
            SettlementId = e.SettlementId,
            CreatedAt = e.CreatedAt,
            ExpiresAt = e.ExpiresAt
        };
    }
}

/// <summary>
/// EF Core implementation of <see cref="IWalletPaymentStore"/> for the CryptoShop example.
/// The <see cref="TeleForge.Payments.Wallet.WalletPaymentService"/> and the
/// <see cref="TeleForge.Payments.Crypto.Observer.TonBlockchainObserverWorker"/> use this
/// to persist and query TON Connect / TON Direct payment sessions.
/// </summary>
public class EfWalletPaymentStore(CryptoShopDbContext db) : IWalletPaymentStore
{
    public async Task SaveSessionAsync(WalletPaymentSession session, CancellationToken ct = default)
    {
        if (await db.WalletSessions.AnyAsync(s => s.SessionId == session.SessionId, ct))
            return;

        db.WalletSessions.Add(ToEntity(session));
        await db.SaveChangesAsync(ct);
    }

    public async Task<WalletPaymentSession?> GetSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var entity = await db.WalletSessions.FindAsync([sessionId], ct);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<WalletPaymentSession?> GetSessionByMemoAsync(string transferMemo, CancellationToken ct = default)
    {
        var entity = await db.WalletSessions
            .FirstOrDefaultAsync(s => s.TransferMemo == transferMemo, ct);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyList<WalletPaymentSession>> GetPendingSessionsAsync(int limit = 50,
        CancellationToken ct = default)
    {
        // SQLite cannot translate DateTimeOffset in ORDER BY; sort client-side after loading.
        var entities = await db.WalletSessions
            .Where(s => s.Status == "Pending")
            .ToListAsync(ct);

        return entities
            .OrderBy(s => s.CreatedAt)
            .Take(limit)
            .Select(ToModel)
            .ToList();
    }

    public async Task UpdateSessionStatusAsync(string sessionId, SettlementStatus status,
        string? txHash, string? senderAddress, CancellationToken ct = default)
    {
        var entity = await db.WalletSessions.FindAsync([sessionId], ct);
        if (entity is null) return;

        entity.Status = status.ToString();
        entity.TransactionHash = txHash ?? entity.TransactionHash;
        entity.SenderAddress = senderAddress ?? entity.SenderAddress;

        await db.SaveChangesAsync(ct);
    }

    private static WalletSessionEntity ToEntity(WalletPaymentSession s)
    {
        return new WalletSessionEntity
        {
            SessionId = s.SessionId,
            PurchaseOrderId = s.PurchaseOrderId,
            UserId = s.UserId,
            Rail = s.Rail.ToString(),
            ExpectedAmount = s.ExpectedAmount,
            Currency = s.Currency.ToString(),
            DestinationAddress = s.DestinationAddress,
            TransferMemo = s.TransferMemo,
            Status = s.Status.ToString(),
            TransactionHash = s.TransactionHash,
            SenderAddress = s.SenderAddress,
            CreatedAt = s.CreatedAt,
            ExpiresAt = s.ExpiresAt
        };
    }

    private static WalletPaymentSession ToModel(WalletSessionEntity e)
    {
        return new WalletPaymentSession
        {
            SessionId = e.SessionId,
            PurchaseOrderId = e.PurchaseOrderId,
            UserId = e.UserId,
            Rail = Enum.Parse<PurchaseRail>(e.Rail),
            ExpectedAmount = e.ExpectedAmount,
            Currency = Enum.Parse<PaymentCurrency>(e.Currency),
            DestinationAddress = e.DestinationAddress,
            TransferMemo = e.TransferMemo,
            Status = Enum.Parse<SettlementStatus>(e.Status),
            TransactionHash = e.TransactionHash,
            SenderAddress = e.SenderAddress,
            CreatedAt = e.CreatedAt,
            ExpiresAt = e.ExpiresAt
        };
    }
}
