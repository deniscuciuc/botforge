using BotForge.Messaging.Abstractions;
using BotForge.Payments.Abstractions;
using BotForge.Payments.Invoice;
using BotForge.Routing.Attributes;
using BotForge.Routing.Handlers;
using Microsoft.EntityFrameworkCore;
using TelegramShop.Data;

namespace TelegramShop.Handlers;

[CallbackQuery("cart:checkout")]
public class CheckoutHandler(
    ITelegramMessageService messaging,
    IInvoiceService invoiceService,
    ShopDbContext db) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var userId = context.UserId!.Value;
        var chatId = context.ChatId!.Value;

        var cartItems = await db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync(ct);

        if (cartItems.Count == 0)
            return CallbackResult.Alert("Your cart is empty!");

        // Create order
        var order = new Order
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Items = cartItems.Select(c => new OrderItem
            {
                ProductId = c.ProductId,
                ProductName = c.Product!.Name,
                Quantity = c.Quantity,
                PriceStars = c.Product.PriceStars
            }).ToList()
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        var total = order.Items.Sum(i => i.PriceStars * i.Quantity);
        var description = string.Join(", ", order.Items.Select(i =>
            i.Quantity > 1 ? $"{i.ProductName} x{i.Quantity}" : i.ProductName));

        // Build Telegram Stars invoice
        var invoice = InvoiceBuilder.Stars()
            .WithTitle("Telegram Shop Order")
            .WithDescription(description.Length > 255 ? description[..252] + "..." : description)
            .WithPayload(new ShopPayload(order.Id))
            .AddPrice("Total", total)
            .Build();

        var result = await invoiceService.SendInvoiceAsync(chatId, invoice, context.BotId, ct);

        if (!result.Success)
        {
            await messaging.CreateMessage(context.BotId)
                .ToChat(chatId)
                .WithHtml($"❌ Failed to create invoice: {result.Error}")
                .SendAsync(ct);

            return CallbackResult.Fail("Invoice creation failed");
        }

        return CallbackResult.Ok();
    }
}

/// <summary>
/// Validates pre-checkout queries for shop purchases.
/// Ensures the order exists and the total matches.
/// </summary>
public class ShopCheckoutValidator(ShopDbContext db) : IPreCheckoutValidator
{
    public async Task<CheckoutValidationResult> ValidateAsync(PreCheckoutContext context, CancellationToken ct)
    {
        var payload = context.GetPayload(ShopPayload.Deserialize);
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == payload.OrderId, ct);

        if (order is null)
            return CheckoutValidationResult.Reject("checkout_error", "Order not found.");

        if (order.TelegramChargeId is not null)
            return CheckoutValidationResult.Reject("checkout_error", "Order already paid.");

        var expectedTotal = order.Items.Sum(i => i.PriceStars * i.Quantity);
        if (context.TotalAmount != expectedTotal)
            return CheckoutValidationResult.Reject("checkout_error", "Price mismatch. Please try again.");

        return CheckoutValidationResult.Approve();
    }
}

/// <summary>
/// Processes successful payments: records charge ID and clears the cart.
/// </summary>
public class ShopPaymentProcessor(
    ITelegramMessageService messaging,
    ShopDbContext db) : IPaymentProcessor
{
    public async Task ProcessAsync(SuccessfulPaymentContext context, CancellationToken ct)
    {
        var payload = context.GetPayload(ShopPayload.Deserialize);

        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == payload.OrderId, ct);

        if (order is not null)
        {
            order.TelegramChargeId = context.ChargeId;
            order.PaidAt = DateTime.UtcNow;

            // Clear cart
            var cartItems = await db.CartItems
                .Where(c => c.UserId == context.UserId)
                .ToListAsync(ct);
            db.CartItems.RemoveRange(cartItems);

            await db.SaveChangesAsync(ct);
        }

        var itemsSummary = order?.Items
                               .Select(i => i.Quantity > 1 ? $"{i.ProductName} x{i.Quantity}" : i.ProductName)
                           ?? ["your items"];

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId)
            .WithHtml(
                $"✅ <b>Payment Successful!</b>\n\n" +
                $"Order #{payload.OrderId}\n" +
                $"Items: {string.Join(", ", itemsSummary)}\n" +
                $"Amount: ⭐{context.TotalAmount}\n\n" +
                $"Thank you for your purchase! 🎉")
            .SendAsync(ct);
    }
}
