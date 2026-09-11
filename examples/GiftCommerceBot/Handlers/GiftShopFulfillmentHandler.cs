using GiftCommerceBot.Data;
using TeleForge.Messaging.Abstractions;
using TeleForge.Payments.Abstractions;

namespace GiftCommerceBot.Handlers;

/// <summary>
/// Fulfillment handler for the "gift-shop" payload prefix.
/// Called by <see cref="TeleForge.Payments.Commerce.PurchaseFulfillmentPipeline"/>
/// after either rail (Stars invoice or inbound Gift) confirms a settlement.
/// Grants the product to the buyer and sends a confirmation message.
/// </summary>
public class GiftShopFulfillmentHandler(
    ITelegramMessageService messaging,
    GiftShopDbContext db) : IPurchaseFulfillmentHandler
{
    public async Task FulfillAsync(PurchaseOrder order, SettlementRecord settlement, CancellationToken ct)
    {
        var payload = GiftShopPayload.Deserialize(
            order.Payload.Split(':')[1..]); // strips the "gift-shop" prefix

        var product = await db.Products.FindAsync([payload.ProductId], ct);

        var railDisplay = settlement.Rail switch
        {
            PurchaseRail.Stars => "⭐ Telegram Stars",
            PurchaseRail.GiftInbound => "🎁 Telegram Gift",
            _ => settlement.Rail.ToString()
        };

        var productName = product?.Name ?? $"Product #{payload.ProductId}";

        // In a real bot, this is where you'd grant access, add a subscription record, etc.
        // For this example we just confirm via message.
        await messaging.CreateMessage(order.BotId)
            .ToChat(order.ChatId)
            .WithHtml(
                $"✅ <b>Purchase Confirmed!</b>\n\n" +
                $"📦 <b>{productName}</b> is now active on your account.\n\n" +
                $"Payment method: {railDisplay}\n" +
                $"Order reference: <code>{order.Id[..8]}</code>")
            .SendAsync(ct);
    }
}
