using BotForge.Messaging.Abstractions;
using BotForge.Payments.Abstractions;
using BotForge.Routing.Attributes;
using BotForge.Routing.Handlers;
using GiftCommerceBot.Data;
using Microsoft.Extensions.Configuration;

namespace GiftCommerceBot.Handlers;

/// <summary>
/// Creates a GiftInbound PurchaseOrder when the user taps "Pay with a Gift".
/// The framework will match the next inbound gift from this user to the pending order.
/// </summary>
[CallbackQuery("shop:buy:gift:{productId}")]
public class BuyWithGiftHandler(
    ITelegramMessageService messaging,
    ICommerceStore commerceStore,
    GiftShopDbContext db) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var productId = context.GetRouteParam<int>("productId");
        var product = await db.Products.FindAsync([productId], ct);

        if (product is null)
            return CallbackResult.Alert("Product not found.");

        var userId = context.UserId!.Value;
        var chatId = context.ChatId!.Value;

        // Create the payload before the order so we have a stable ID
        var orderId = Guid.NewGuid().ToString("N");
        var payload = new GiftShopPayload(productId, orderId);

        var order = new PurchaseOrder
        {
            Id = orderId,
            UserId = userId,
            ChatId = chatId,
            BotId = context.BotId,
            Payload = payload.Serialize(),
            Amount = product.PriceStars,
            Currency = PaymentCurrency.Xtr,
            Rail = PurchaseRail.GiftInbound,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        };

        await commerceStore.SaveOrderAsync(order, ct);

        await messaging.CreateMessage(context.BotId)
            .ToChat(chatId)
            .WithHtml(
                $"🎁 <b>Gift Payment Initiated</b>\n\n" +
                $"To purchase <b>{product.Name}</b>, send a Telegram Gift " +
                $"worth at least ⭐<b>{product.PriceStars} Stars</b> to this chat " +
                $"within the next 24 hours.\n\n" +
                $"Once the gift is received, your order will be fulfilled automatically.\n\n" +
                $"Order reference: <code>{orderId[..8]}</code>")
            .SendAsync(ct);

        return CallbackResult.Ok();
    }
}

/// <summary>
/// Routes inbound regular gift service messages (Message.Gift) to IGiftIntakeService.
/// The service matches the gift to any pending GiftInbound PurchaseOrder from the sender.
/// </summary>
[GiftMessage]
public class GiftReceivedHandler(IGiftIntakeService giftIntakeService) : IGiftMessageHandler
{
    public async Task HandleAsync(GiftMessageContext context, CancellationToken ct)
    {
        // Build a stable OwnedGiftId for non-Business messages (no owned_gift_id in regular chats)
        var ownedGiftId = context.OwnedGiftId
                          ?? $"msg:{context.Message.MessageId}:{context.GiftInfo.Gift.Id}";

        var giftEvent = new ReceivedGiftEvent
        {
            BotId = context.BotId,
            OwnedGiftId = ownedGiftId,
            GiftId = context.GiftInfo.Gift.Id,
            GiftType = GiftType.Regular,
            SenderUserId = context.UserId,
            ConvertStarCount = context.ConvertStarCount,
            CanBeUpgraded = context.CanBeUpgraded,
            GiftedAt = context.Message.Date
        };

        await giftIntakeService.ProcessReceivedGiftAsync(giftEvent, ct);
    }
}

/// <summary>
/// Routes inbound unique (NFT) gift service messages (Message.UniqueGift) to IGiftIntakeService.
/// </summary>
[UniqueGiftMessage]
public class UniqueGiftReceivedHandler(IGiftIntakeService giftIntakeService) : IUniqueGiftMessageHandler
{
    public async Task HandleAsync(UniqueGiftMessageContext context, CancellationToken ct)
    {
        var ownedGiftId = context.OwnedGiftId
                          ?? $"unique:msg:{context.Message.MessageId}";

        var giftEvent = new ReceivedGiftEvent
        {
            BotId = context.BotId,
            OwnedGiftId = ownedGiftId,
            GiftId = context.UniqueGiftInfo.Gift.Name,
            GiftType = GiftType.Unique,
            SenderUserId = context.UserId,
            ConvertStarCount = null, // unique gifts can't be converted to Stars
            CanBeUpgraded = false,
            GiftedAt = context.Message.Date
        };

        await giftIntakeService.ProcessReceivedGiftAsync(giftEvent, ct);
    }
}

/// <summary>
/// /reconcile — Admin command: polls the Business account's owned gifts list
/// to catch any gifts that may have been missed by real-time service messages.
/// Requires the bot to have a Telegram Business account connection configured.
/// </summary>
[TelegramCommand("/reconcile", "Reconcile Business account gifts (admin)")]
public class ReconcileGiftsCommand(
    IGiftIntakeService giftIntakeService,
    IConfiguration configuration,
    ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var businessConnectionId = configuration["Telegram:BusinessConnectionId"];
        if (string.IsNullOrEmpty(businessConnectionId))
        {
            await messaging.CreateMessage(context.BotId)
                .ToChat(context.ChatId!.Value)
                .WithHtml("⚠️ <code>Telegram:BusinessConnectionId</code> is not configured.")
                .SendAsync(ct);
            return CommandResult.Ok();
        }

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml("🔄 Reconciling Business account gifts…")
            .SendAsync(ct);

        await giftIntakeService.ReconcileBusinessGiftsAsync(context.BotId, businessConnectionId, ct);

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml("✅ Reconciliation complete.")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}
