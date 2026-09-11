using Microsoft.Extensions.Logging;
using TeleForge.Payments.Abstractions;

namespace TeleForge.Payments.Gift;

/// <summary>
/// Processes inbound gift events from service messages or Business account polling.
/// Matches each received gift to a pending <see cref="PurchaseOrder"/> and, if matched,
/// emits a <see cref="SettlementRecord"/> and triggers fulfillment.
/// </summary>
public class GiftIntakeService(
    IGiftIntakeStore intakeStore,
    ICommerceStore commerceStore,
    IGiftService giftService,
    Commerce.PurchaseFulfillmentPipeline fulfillmentPipeline,
    ILogger<GiftIntakeService> logger)
    : IGiftIntakeService
{
    public async Task ProcessReceivedGiftAsync(ReceivedGiftEvent giftEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(giftEvent);

        // Idempotency: skip already-processed gifts
        var existing = await intakeStore.GetByOwnedGiftIdAsync(giftEvent.OwnedGiftId, ct).ConfigureAwait(false);
        if (existing is not null && existing.Status != GiftIntakeStatus.Pending)
        {
            logger.LogDebug("Gift {OwnedGiftId} already processed (status: {Status}), skipping",
                giftEvent.OwnedGiftId, existing.Status);
            return;
        }

        var record = new GiftIntakeRecord
        {
            OwnedGiftId = giftEvent.OwnedGiftId,
            GiftId = giftEvent.GiftId,
            GiftType = giftEvent.GiftType,
            SenderUserId = giftEvent.SenderUserId,
            ConvertStarCount = giftEvent.ConvertStarCount,
            CanBeUpgraded = giftEvent.CanBeUpgraded,
            GiftedAt = giftEvent.GiftedAt
        };

        if (existing is null)
            await intakeStore.SaveIntakeAsync(record, ct).ConfigureAwait(false);

        // Match to a pending order from the sender
        PurchaseOrder? matchedOrder = null;
        if (giftEvent.SenderUserId.HasValue)
        {
            var pendingOrders = await commerceStore.GetPendingOrdersAsync(giftEvent.SenderUserId.Value, ct).ConfigureAwait(false);
            matchedOrder = pendingOrders.FirstOrDefault(o =>
                o.Rail == PurchaseRail.GiftInbound &&
                o.Status == SettlementStatus.Pending &&
                IsSatisfiedByGift(o, giftEvent));
        }

        if (matchedOrder is null)
        {
            logger.LogInformation(
                "Received gift {OwnedGiftId} from user {Sender} has no matching pending order — recording as unmatched",
                giftEvent.OwnedGiftId, giftEvent.SenderUserId);
            await intakeStore.UpdateIntakeStatusAsync(record.Id, GiftIntakeStatus.Rejected, null, ct).ConfigureAwait(false);
            return;
        }

        await intakeStore.UpdateIntakeStatusAsync(record.Id, GiftIntakeStatus.Accepted, matchedOrder.Id, ct).ConfigureAwait(false);

        var settlement = new SettlementRecord
        {
            SettlementId = $"gift:{giftEvent.OwnedGiftId}",
            PurchaseOrderId = matchedOrder.Id,
            Rail = PurchaseRail.GiftInbound,
            UserId = giftEvent.SenderUserId!.Value,
            Amount = giftEvent.ConvertStarCount ?? 0,
            Currency = PaymentCurrency.Xtr,
            RawData = giftEvent.OwnedGiftId
        };

        await fulfillmentPipeline.FulfillAsync(matchedOrder, settlement, ct).ConfigureAwait(false);

        logger.LogInformation(
            "Inbound gift {OwnedGiftId} matched to order {OrderId} and fulfilled",
            giftEvent.OwnedGiftId, matchedOrder.Id);
    }

    public async Task ReconcileBusinessGiftsAsync(string botId, string businessConnectionId,
        CancellationToken ct = default)
    {
        string? offset = null;
        var processed = 0;

        do
        {
            var page = await giftService.GetBusinessAccountGiftsAsync(
                botId, businessConnectionId, offset, 100, ct).ConfigureAwait(false);

            foreach (var gift in page.Gifts)
            {
                var giftEvent = new ReceivedGiftEvent
                {
                    BotId = botId,
                    OwnedGiftId = gift.GiftId,
                    GiftId = gift.GiftId,
                    GiftType = gift.Type,
                    SenderUserId = gift.SenderUserId,
                    ConvertStarCount = gift is OwnedGiftRegularInfo r ? r.ConvertStarCount : null,
                    CanBeUpgraded = gift is OwnedGiftRegularInfo reg && reg.CanUpgrade,
                    GiftedAt = gift.Date ?? DateTimeOffset.UtcNow,
                    IsFromPolling = true,
                    BusinessConnectionId = businessConnectionId
                };

                await ProcessReceivedGiftAsync(giftEvent, ct).ConfigureAwait(false);
                processed++;
            }

            offset = page.NextOffset;
        } while (!string.IsNullOrEmpty(offset));

        logger.LogInformation(
            "Business gift reconciliation complete: {Count} gifts processed for connection {BizConn}",
            processed, businessConnectionId);
    }

    private static bool IsSatisfiedByGift(PurchaseOrder order, ReceivedGiftEvent gift)
    {
        // For gift inbound orders we match by currency (Stars via convert value) and amount.
        // Metadata key "gift_id" allows restricting to a specific catalog gift if the bot set it.
        if (order.Metadata.TryGetValue("gift_id", out var requiredGiftId) &&
            gift.GiftId != requiredGiftId)
            return false;

        if (order.Currency == PaymentCurrency.Xtr && gift.ConvertStarCount.HasValue)
            return gift.ConvertStarCount.Value >= order.Amount;

        // If no star value is attached, accept the gift and record amount as 0 to let application logic decide
        return true;
    }
}
