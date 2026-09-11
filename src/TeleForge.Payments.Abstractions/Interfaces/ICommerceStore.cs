namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Manages the commerce store: purchase orders and settlement records across all rails.
/// Implemented by the consuming application.
/// </summary>
public interface ICommerceStore
{
    // Purchase Orders
    Task SaveOrderAsync(PurchaseOrder order, CancellationToken ct = default);
    Task<PurchaseOrder?> GetOrderAsync(string orderId, CancellationToken ct = default);
    Task<IReadOnlyList<PurchaseOrder>> GetPendingOrdersAsync(long userId, CancellationToken ct = default);

    Task UpdateOrderStatusAsync(string orderId, SettlementStatus status, string? settlementId,
        CancellationToken ct = default);

    // Settlement Records
    Task SaveSettlementAsync(SettlementRecord settlement, CancellationToken ct = default);
    Task<SettlementRecord?> GetSettlementAsync(string settlementId, CancellationToken ct = default);
    Task<bool> IsSettledAsync(string settlementId, CancellationToken ct = default);
}
