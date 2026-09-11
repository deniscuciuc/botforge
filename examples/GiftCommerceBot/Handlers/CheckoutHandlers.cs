using GiftCommerceBot.Data;
using TeleForge.Messaging.Abstractions;
using TeleForge.Payments.Abstractions;
using TeleForge.Payments.Commerce;
using TeleForge.Payments.Invoice;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Handlers;

namespace GiftCommerceBot.Handlers;

/// <summary>
/// Creates a Telegram Stars invoice when the user taps "Pay with Stars".
/// A PurchaseOrder is saved so fulfillment runs through the same pipeline
/// as the GiftInbound rail.
/// </summary>
[CallbackQuery("shop:buy:stars:{productId}")]
public class BuyWithStarsHandler(
    IInvoiceService invoiceService,
    ICommerceStore commerceStore,
    ITelegramMessageService messaging,
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

        var orderId = Guid.NewGuid().ToString("N");
        var payload = new GiftShopPayload(productId, orderId);

        // Persist the PurchaseOrder — the Stars processor will update it on success,
        // and the fulfillment handler will grant the product.
        var order = new PurchaseOrder
        {
            Id = orderId,
            UserId = userId,
            ChatId = chatId,
            BotId = context.BotId,
            Payload = payload.Serialize(),
            Amount = product.PriceStars,
            Currency = PaymentCurrency.Xtr,
            Rail = PurchaseRail.Stars,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        await commerceStore.SaveOrderAsync(order, ct);

        var invoice = InvoiceBuilder.Stars()
            .WithTitle(product.Name)
            .WithDescription(product.Description.Length > 255
                ? product.Description[..252] + "..."
                : product.Description)
            .WithPayload(payload)
            .AddPrice(product.Name, product.PriceStars)
            .Build();

        var result = await invoiceService.SendInvoiceAsync(chatId, invoice, context.BotId, ct);

        if (!result.Success)
        {
            await messaging.CreateMessage(context.BotId)
                .ToChat(chatId)
                .WithHtml($"❌ Failed to create invoice: {result.Error}")
                .SendAsync(ct);

            return CallbackResult.Fail("invoice_error");
        }

        return CallbackResult.Ok();
    }
}

/// <summary>
/// Pre-checkout validator — verifies the order exists and the amount matches.
/// Registered for the "gift-shop" payload prefix.
/// </summary>
public class GiftShopCheckoutValidator(ICommerceStore commerceStore) : IPreCheckoutValidator
{
    public async Task<CheckoutValidationResult> ValidateAsync(PreCheckoutContext context, CancellationToken ct)
    {
        var payload = context.GetPayload(GiftShopPayload.Deserialize);
        var order = await commerceStore.GetOrderAsync(payload.OrderId, ct);

        if (order is null)
            return CheckoutValidationResult.Reject("checkout_error", "Order not found.");

        if (order.Status != SettlementStatus.Pending)
            return CheckoutValidationResult.Reject("checkout_error", "This order has already been processed.");

        if (context.TotalAmount != order.Amount)
            return CheckoutValidationResult.Reject("checkout_error", "Price mismatch — please try again.");

        return CheckoutValidationResult.Approve();
    }
}

/// <summary>
/// Stars payment processor — records the Telegram charge ID and triggers the unified fulfillment
/// pipeline, so both the Stars rail and the GiftInbound rail run through
/// <see cref="GiftShopFulfillmentHandler"/>.
/// Registered for the "gift-shop" payload prefix.
/// </summary>
public class GiftShopStarsProcessor(
    ICommerceStore commerceStore,
    PurchaseFulfillmentPipeline fulfillmentPipeline) : IPaymentProcessor
{
    public async Task ProcessAsync(SuccessfulPaymentContext context, CancellationToken ct)
    {
        var payload = context.GetPayload(GiftShopPayload.Deserialize);
        var order = await commerceStore.GetOrderAsync(payload.OrderId, ct);

        if (order is null)
            return;

        var settlement = new SettlementRecord
        {
            SettlementId = $"stars:{context.ChargeId}",
            PurchaseOrderId = order.Id,
            Rail = PurchaseRail.Stars,
            UserId = context.UserId,
            Amount = context.TotalAmount,
            Currency = PaymentCurrency.Xtr,
            RawData = context.ChargeId
        };

        // FulfillAsync saves the settlement, updates the order status to Confirmed,
        // and calls GiftShopFulfillmentHandler to grant the product and notify the user.
        await fulfillmentPipeline.FulfillAsync(order, settlement, ct);
    }
}
