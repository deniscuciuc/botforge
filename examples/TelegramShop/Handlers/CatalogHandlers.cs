using Microsoft.EntityFrameworkCore;
using TeleForge.Messaging.Abstractions;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Handlers;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramShop.Data;

namespace TelegramShop.Handlers;

[TelegramCommand("/start", "Start the shop bot")]
public class StartHandler(ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "🛍️ <b>Welcome to Telegram Shop!</b>\n\n" +
                "Browse our catalog and pay with Telegram Stars ⭐\n\n" +
                "/catalog — Browse products\n" +
                "/cart — View your cart\n" +
                "/orders — Order history")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

[TelegramCommand("/catalog", "Browse available products")]
public class CatalogHandler(ITelegramMessageService messaging, ShopDbContext db) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var products = await db.Products.ToListAsync(ct);

        var text = "🛍️ <b>Catalog</b>\n\nSelect a product to view details:";
        var buttons = products
            .Select(p => new[]
                { InlineKeyboardButton.WithCallbackData($"{p.Name} — ⭐{p.PriceStars}", $"product:{p.Id}") })
            .ToArray();

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(text)
            .WithInlineKeyboard(new InlineKeyboardMarkup(buttons))
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

[CallbackQuery("product:{id}")]
public class ProductDetailHandler(ITelegramMessageService messaging, ShopDbContext db) : ICallbackQueryHandler
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
            $"💰 Price: ⭐{product.PriceStars}";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🛒 Add to Cart", $"cart:add:{product.Id}") },
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Back to Catalog", "catalog:back") }
        });

        var messageId = context.UpdateContext.RawUpdate.CallbackQuery?.Message?.MessageId;
        var msg = messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(text)
            .WithInlineKeyboard(keyboard);

        if (messageId.HasValue)
            msg.EditMessage(messageId.Value);

        await msg.SendAsync(ct);
        return CallbackResult.Ok();
    }
}

[CallbackQuery("catalog:back")]
public class CatalogBackHandler(ITelegramMessageService messaging, ShopDbContext db) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var products = await db.Products.ToListAsync(ct);
        var text = "🛍️ <b>Catalog</b>\n\nSelect a product to view details:";
        var buttons = products
            .Select(p => new[]
                { InlineKeyboardButton.WithCallbackData($"{p.Name} — ⭐{p.PriceStars}", $"product:{p.Id}") })
            .ToArray();

        var messageId = context.UpdateContext.RawUpdate.CallbackQuery?.Message?.MessageId;
        var msg = messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(text)
            .WithInlineKeyboard(new InlineKeyboardMarkup(buttons));

        if (messageId.HasValue)
            msg.EditMessage(messageId.Value);

        await msg.SendAsync(ct);
        return CallbackResult.Ok();
    }
}
