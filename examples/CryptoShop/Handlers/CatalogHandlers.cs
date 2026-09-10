using BotForge.Messaging.Abstractions;
using BotForge.Routing.Attributes;
using BotForge.Routing.Handlers;
using CryptoShop.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.ReplyMarkups;

namespace CryptoShop.Handlers;

/// <summary>
/// /start — welcome message and overview of payment rails.
/// </summary>
[TelegramCommand("/start", "Start the Crypto Shop bot")]
public class StartHandler(ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "💎 <b>Welcome to Crypto Shop!</b>\n\n" +
                "This bot demonstrates all available payment rails:\n\n" +
                "  ⭐ <b>Telegram Stars</b> — native Stars invoice\n" +
                "  🔗 <b>TON Connect</b> — Mini App wallet deep-link flow\n" +
                "  📥 <b>TON Direct</b> — deposit to address with memo comment\n\n" +
                "Use /catalog to browse products.\n" +
                "Use /help for all commands.")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

/// <summary>
/// /help — list all available commands.
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
                "/status — Check a pending payment status\n" +
                "/help — This message")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

/// <summary>
/// /catalog — list all products as inline buttons.
/// </summary>
[TelegramCommand("/catalog", "Browse available products")]
public class CatalogHandler(ITelegramMessageService messaging, CryptoShopDbContext db) : ICommandHandler
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
            .WithHtml("🛍️ <b>Catalog</b>\n\nChoose a product to see pricing and payment options:")
            .WithInlineKeyboard(new InlineKeyboardMarkup(buttons))
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

/// <summary>
/// Callback: product detail with prices in all currencies and a checkout button.
/// </summary>
[CallbackQuery("shop:product:{id}")]
public class ProductDetailHandler(ITelegramMessageService messaging, CryptoShopDbContext db) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var productId = context.GetRouteParam<int>("id");
        var product = await db.Products.FindAsync([productId], ct);

        if (product is null)
            return CallbackResult.Alert("Product not found.");

        var tonDisplay = $"{product.PriceTon / 1_000_000_000.0:0.###} TON";
        var usdtDisplay = $"{product.PriceUsdt / 1_000_000.0:0.##} USDT";

        var text =
            $"📦 <b>{product.Name}</b>\n\n" +
            $"{product.Description}\n\n" +
            $"💰 Prices:\n" +
            $"  ⭐ {product.PriceStars} Stars\n" +
            $"  💎 {tonDisplay}\n" +
            $"  💵 {usdtDisplay}\n\n" +
            "Choose your preferred payment method:";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("⭐ Pay with Stars", $"shop:checkout:{product.Id}:stars") },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔗 Pay with TON Connect",
                    $"shop:checkout:{product.Id}:ton-connect")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📥 Pay with TON Direct",
                    $"shop:checkout:{product.Id}:ton-direct")
            },
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
/// Back-to-catalog inline button.
/// </summary>
[CallbackQuery("shop:catalog")]
public class BackToCatalogHandler(ITelegramMessageService messaging, CryptoShopDbContext db) : ICallbackQueryHandler
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
            .WithHtml("🛍️ <b>Catalog</b>\n\nChoose a product to see pricing and payment options:")
            .WithInlineKeyboard(new InlineKeyboardMarkup(buttons));

        if (msgId.HasValue)
            msg.EditMessage(msgId.Value);

        await msg.SendAsync(ct);
        return CallbackResult.Ok();
    }
}
