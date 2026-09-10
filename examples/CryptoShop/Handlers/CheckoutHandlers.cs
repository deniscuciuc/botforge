using BotForge.Messaging.Abstractions;
using BotForge.Payments.Abstractions;
using BotForge.Payments.Commerce;
using BotForge.Payments.Invoice;
using BotForge.Routing.Attributes;
using BotForge.Routing.Handlers;
using CryptoShop.Data;

namespace CryptoShop.Handlers;

/// <summary>
/// Checkout via Telegram Stars invoice.
/// Creates a PurchaseOrder and sends a Stars invoice.
/// Route: shop:checkout:{productId}:stars
/// </summary>
[CallbackQuery("shop:checkout:{productId}:stars")]
public class StarsCheckoutHandler(
    IInvoiceService invoiceService,
    ICommerceStore commerceStore,
    ITelegramMessageService messaging,
    CryptoShopDbContext db) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var productId = context.GetRouteParam<int>("productId");
        var product = await db.Products.FindAsync([productId], ct);

        if (product is null)
            return CallbackResult.Alert("Product not found.");

        var orderId = Guid.NewGuid().ToString("N");
        var payload = new CryptoShopPayload(productId, orderId);

        var order = new PurchaseOrder
        {
            Id = orderId,
            UserId = context.UserId!.Value,
            ChatId = context.ChatId!.Value,
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

        var result = await invoiceService.SendInvoiceAsync(context.ChatId!.Value, invoice, context.BotId, ct);

        if (!result.Success)
        {
            await messaging.CreateMessage(context.BotId)
                .ToChat(context.ChatId!.Value)
                .WithHtml($"❌ Failed to create invoice: {result.Error}")
                .SendAsync(ct);
            return CallbackResult.Fail("invoice_error");
        }

        return CallbackResult.Ok();
    }
}

/// <summary>
/// Pre-checkout validator for Stars payments.
/// Registered for the "crypto-shop" payload prefix.
/// </summary>
public class CryptoShopCheckoutValidator(ICommerceStore commerceStore) : IPreCheckoutValidator
{
    public async Task<CheckoutValidationResult> ValidateAsync(PreCheckoutContext context, CancellationToken ct)
    {
        var payload = context.GetPayload(CryptoShopPayload.Deserialize);
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
/// Stars payment processor — triggers unified fulfillment pipeline.
/// Registered for the "crypto-shop" payload prefix.
/// </summary>
public class CryptoShopStarsProcessor(
    ICommerceStore commerceStore,
    PurchaseFulfillmentPipeline fulfillmentPipeline) : IPaymentProcessor
{
    public async Task ProcessAsync(SuccessfulPaymentContext context, CancellationToken ct)
    {
        var payload = context.GetPayload(CryptoShopPayload.Deserialize);
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

        await fulfillmentPipeline.FulfillAsync(order, settlement, ct);
    }
}
