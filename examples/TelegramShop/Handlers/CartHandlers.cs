using BotForge.Messaging.Abstractions;
using BotForge.Routing.Attributes;
using BotForge.Routing.Handlers;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramShop.Data;

namespace TelegramShop.Handlers;

[CallbackQuery("cart:add:{id}")]
public class AddToCartHandler(ShopDbContext db) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var productId = context.GetRouteParam<int>("id");
        var userId = context.UserId!.Value;

        var product = await db.Products.FindAsync([productId], ct);
        if (product is null)
            return CallbackResult.Alert("Product not found.");

        var existing = await db.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId, ct);

        if (existing is not null)
            existing.Quantity++;
        else
            db.CartItems.Add(new CartItem
            {
                UserId = userId,
                ProductId = productId,
                Quantity = 1
            });

        await db.SaveChangesAsync(ct);

        return CallbackResult.Alert($"✅ {product.Name} added to cart!");
    }
}

[TelegramCommand("/cart", "View your shopping cart")]
public class ViewCartHandler(ITelegramMessageService messaging, ShopDbContext db) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var userId = context.UserId!.Value;
        var items = await db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync(ct);

        if (items.Count == 0)
        {
            await messaging.CreateMessage(context.BotId)
                .ToChat(context.ChatId!.Value)
                .WithHtml("🛒 Your cart is empty.\n\nUse /catalog to browse products.")
                .SendAsync(ct);

            return CommandResult.Ok();
        }

        var total = items.Sum(i => i.Product!.PriceStars * i.Quantity);
        var lines = items.Select(i =>
            $"  • {i.Product!.Name} x{i.Quantity} — ⭐{i.Product.PriceStars * i.Quantity}");
        var text = $"🛒 <b>Your Cart</b>\n\n{string.Join("\n", lines)}\n\n💰 <b>Total: ⭐{total}</b>";

        var buttons = new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("💳 Checkout", "cart:checkout") },
            new[] { InlineKeyboardButton.WithCallbackData("🗑 Clear Cart", "cart:clear") }
        };

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(text)
            .WithInlineKeyboard(new InlineKeyboardMarkup(buttons))
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

[CallbackQuery("cart:clear")]
public class ClearCartHandler(ITelegramMessageService messaging, ShopDbContext db) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var userId = context.UserId!.Value;
        var items = await db.CartItems.Where(c => c.UserId == userId).ToListAsync(ct);
        db.CartItems.RemoveRange(items);
        await db.SaveChangesAsync(ct);

        var messageId = context.UpdateContext.RawUpdate.CallbackQuery?.Message?.MessageId;
        var msg = messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml("🗑 Cart cleared.\n\nUse /catalog to browse products.");

        if (messageId.HasValue)
            msg.EditMessage(messageId.Value);

        await msg.SendAsync(ct);
        return CallbackResult.Ok();
    }
}
