using System.Globalization;
using GiftCommerceBot.Data;
using Microsoft.EntityFrameworkCore;
using TeleForge.Messaging.Abstractions;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Handlers;
using Telegram.Bot.Types.ReplyMarkups;

namespace GiftCommerceBot.Handlers;

/// <summary>
/// /start — greet and show available payment methods.
/// </summary>
[TelegramCommand("/start", "Start the Gift Shop bot")]
public class StartHandler(ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "🎁 <b>Welcome to Gift Shop!</b>\n\n" +
                "This bot lets you purchase digital items using:\n" +
                "  ⭐ <b>Telegram Stars</b> — instant invoice payment\n" +
                "  🎀 <b>Telegram Gifts</b> — send a gift to pay\n\n" +
                "Use /catalog to browse products.\n" +
                "Use /help to see all commands.")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

/// <summary>
/// /help — list all commands.
/// </summary>
[TelegramCommand("/help", "Show available commands")]
public class HelpHandler(ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "📖 <b>Commands</b>\n\n" +
                "/catalog — Browse all products\n" +
                "/orders — View your purchase history\n" +
                "/help — This message")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

/// <summary>
/// /catalog — List all available products with Stars and Gift options.
/// </summary>
[TelegramCommand("/catalog", "Browse products")]
public class CatalogHandler(ITelegramMessageService messaging, GiftShopDbContext db) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var products = await db.Products.Where(p => p.IsAvailable).ToListAsync(ct);

        var buttons = products
            .Select(p => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{p.Name} — ⭐{p.PriceStars}", $"shop:product:{p.Id}")
            })
            .ToArray();

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml("🛍️ <b>Catalog</b>\n\nChoose a product to see payment options:")
            .WithInlineKeyboard(new InlineKeyboardMarkup(buttons))
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

/// <summary>
/// Callback: display product details + payment option buttons.
/// </summary>
[CallbackQuery("shop:product:{id}")]
public class ProductDetailHandler(ITelegramMessageService messaging, GiftShopDbContext db) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var productId = context.GetRouteParam<int>("id");
        var product = await db.Products.FindAsync([productId], ct);

        if (product is null)
            return CallbackResult.Alert("Product not found.");

        var text =
            $"📦 <b>{product.Name}</b>\n\n" +
            $"{product.Description}\n\n" +
            $"💰 Price: ⭐{product.PriceStars}\n\n" +
            "Choose how you'd like to pay:";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("⭐ Pay with Stars", $"shop:buy:stars:{product.Id}") },
            new[] { InlineKeyboardButton.WithCallbackData("🎁 Pay with a Gift", $"shop:buy:gift:{product.Id}") },
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Back to Catalog", "shop:catalog") }
        });

        var msgId = context.UpdateContext.RawUpdate.CallbackQuery?.Message?.MessageId;
        var msg = messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(text)
            .WithInlineKeyboard(keyboard);

        if (msgId.HasValue)
            msg.EditMessage(msgId.Value);

        await msg.SendAsync(ct);
        return CallbackResult.Ok();
    }
}

/// <summary>
/// Back-to-catalog button — re-renders the catalog inline.
/// </summary>
[CallbackQuery("shop:catalog")]
public class BackToCatalogHandler(ITelegramMessageService messaging, GiftShopDbContext db) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var products = await db.Products.Where(p => p.IsAvailable).ToListAsync(ct);

        var buttons = products
            .Select(p => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{p.Name} — ⭐{p.PriceStars}", $"shop:product:{p.Id}")
            })
            .ToArray();

        var msgId = context.UpdateContext.RawUpdate.CallbackQuery?.Message?.MessageId;
        var msg = messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml("🛍️ <b>Catalog</b>\n\nChoose a product to see payment options:")
            .WithInlineKeyboard(new InlineKeyboardMarkup(buttons));

        if (msgId.HasValue)
            msg.EditMessage(msgId.Value);

        await msg.SendAsync(ct);
        return CallbackResult.Ok();
    }
}

/// <summary>
/// /orders — show purchase history for the current user.
/// </summary>
[TelegramCommand("/orders", "View your purchase history")]
public class OrderHistoryHandler(ITelegramMessageService messaging, GiftShopDbContext db) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var userId = context.UserId!.Value;
        var orders = await db.PurchaseOrders
            .Where(o => o.UserId == userId && o.Status == "Confirmed")
            .OrderByDescending(o => o.PaidAt)
            .Take(10)
            .ToListAsync(ct);

        if (orders.Count == 0)
        {
            await messaging.CreateMessage(context.BotId)
                .ToChat(context.ChatId!.Value)
                .WithHtml("📭 You have no completed purchases yet.\n\nUse /catalog to browse products.")
                .SendAsync(ct);

            return CommandResult.Ok();
        }

        var lines = orders.Select(o =>
        {
            var rail = o.Rail == "GiftInbound" ? "🎁 Gift" : "⭐ Stars";
            var date = o.PaidAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—";
            return $"• {o.Payload} — {rail} — {date}";
        });

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml($"📋 <b>Your Orders</b>\n\n{string.Join('\n', lines)}")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}
