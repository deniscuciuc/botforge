using System.Globalization;
using BotForge.Messaging.Abstractions;
using BotForge.Payments.Abstractions;
using CryptoShop.Data;

namespace CryptoShop.Handlers;

/// <summary>
/// Fulfillment handler for the "crypto-shop" payload prefix.
/// Called by <see cref="BotForge.Payments.Commerce.PurchaseFulfillmentPipeline"/>
/// after <b>any</b> payment rail confirms a settlement:
///   • ⭐ Stars invoice
///   • 🔗 TON Connect (triggered by TonBlockchainObserverWorker)
///   • 📥 TON Direct   (triggered by TonBlockchainObserverWorker)
///
/// This is the single point of fulfillment logic — grant the product,
/// update any app-specific state, and notify the buyer.
/// </summary>
public class CryptoShopFulfillmentHandler(
    ITelegramMessageService messaging,
    CryptoShopDbContext db) : IPurchaseFulfillmentHandler
{
    public async Task FulfillAsync(PurchaseOrder order, SettlementRecord settlement, CancellationToken ct)
    {
        var fields = order.Payload.Split(':');
        // Payload format: "crypto-shop:{productId}:{orderId}" → fields[1] = productId
        var productId = int.Parse(fields[1], CultureInfo.InvariantCulture);
        var product = await db.Products.FindAsync([productId], ct);

        var railDisplay = settlement.Rail switch
        {
            PurchaseRail.Stars => "⭐ Telegram Stars",
            PurchaseRail.TonConnect => "🔗 TON Connect",
            PurchaseRail.TonDirect => "📥 TON Direct",
            _ => settlement.Rail.ToString()
        };

        var amountDisplay = settlement.Currency switch
        {
            PaymentCurrency.Xtr => $"⭐{settlement.Amount}",
            PaymentCurrency.Ton => $"{settlement.Amount / 1_000_000_000.0:0.###} TON",
            PaymentCurrency.Usdt => $"{settlement.Amount / 1_000_000.0:0.##} USDT",
            _ => settlement.Amount.ToString(CultureInfo.InvariantCulture)
        };

        var productName = product?.Name ?? $"Product #{productId}";

        // In a real bot: grant subscription, unlock feature, insert DB record, etc.
        await messaging.CreateMessage(order.BotId)
            .ToChat(order.ChatId)
            .WithHtml(
                $"✅ <b>Purchase Confirmed!</b>\n\n" +
                $"📦 <b>{productName}</b> is now active on your account.\n\n" +
                $"Payment: {railDisplay} — {amountDisplay}\n" +
                $"Settlement: <code>{settlement.SettlementId[..Math.Min(16, settlement.SettlementId.Length)]}</code>\n" +
                $"Order ref: <code>{order.Id[..8]}</code>")
            .SendAsync(ct);
    }
}
