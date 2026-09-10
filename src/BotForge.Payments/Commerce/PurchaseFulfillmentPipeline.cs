using BotForge.Payments.Abstractions;
using Microsoft.Extensions.Logging;

namespace BotForge.Payments.Commerce;

/// <summary>
/// Orchestrates fulfillment after any rail confirms a settlement.
/// Checks idempotency, resolves fulfillment handlers by payload prefix, and
/// updates the order status in the commerce store.
/// </summary>
public class PurchaseFulfillmentPipeline(
    PurchaseFulfillmentRegistry registry,
    ICommerceStore commerceStore,
    IServiceProvider services,
    ILogger<PurchaseFulfillmentPipeline> logger)
{
    /// <summary>
    /// Execute fulfillment for the given order and settlement.
    /// Idempotent: if the order is already <see cref="SettlementStatus.Confirmed"/> this is a no-op.
    /// </summary>
    public async Task FulfillAsync(PurchaseOrder order, SettlementRecord settlement, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(settlement);

        // Idempotency guard — check in-process first
        if (order.Status == SettlementStatus.Confirmed)
        {
            logger.LogDebug("Order {OrderId} already confirmed, skipping duplicate fulfillment", order.Id);
            return;
        }

        // Verify persistence-level idempotency
        if (await commerceStore.IsSettledAsync(settlement.SettlementId, ct).ConfigureAwait(false))
        {
            logger.LogDebug("Settlement {SettlementId} already recorded, skipping duplicate fulfillment",
                settlement.SettlementId);
            return;
        }

        await commerceStore.SaveSettlementAsync(settlement, ct).ConfigureAwait(false);
        await commerceStore.UpdateOrderStatusAsync(order.Id, SettlementStatus.Confirmed,
            settlement.SettlementId, ct).ConfigureAwait(false);

        var prefix = ExtractPrefix(order.Payload);
        var handlers = registry.GetHandlers(prefix, services);

        if (handlers.Count == 0)
        {
            logger.LogWarning(
                "No fulfillment handlers registered for payload prefix '{Prefix}' (order {OrderId}, rail {Rail})",
                prefix, order.Id, settlement.Rail);
            return;
        }

        foreach (var handler in handlers)
            try
            {
                await handler.FulfillAsync(order, settlement, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Fulfillment handler {Handler} failed for order {OrderId}", handler.GetType().Name, order.Id);
                throw;
            }

        logger.LogInformation(
            "Order {OrderId} fulfilled via {Rail} (settlement {SettlementId})",
            order.Id, settlement.Rail, settlement.SettlementId);
    }

    private static string ExtractPrefix(string payload)
    {
        var colon = payload.IndexOf(':');
        return colon < 0 ? payload : payload[..colon];
    }
}
