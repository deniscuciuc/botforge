namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Processes a settlement from any payment rail and triggers fulfillment.
/// Implementations handle the product/order fulfillment after a settlement is confirmed.
/// Register via <see cref="PurchaseFulfillmentRegistry"/>.
/// </summary>
public interface IPurchaseFulfillmentHandler
{
    Task FulfillAsync(PurchaseOrder order, SettlementRecord settlement, CancellationToken ct = default);
}
