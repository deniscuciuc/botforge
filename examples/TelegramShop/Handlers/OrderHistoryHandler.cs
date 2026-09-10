using System.Globalization;
using BotForge.Messaging.Abstractions;
using BotForge.Routing.Attributes;
using BotForge.Routing.Handlers;
using Microsoft.EntityFrameworkCore;
using TelegramShop.Data;

namespace TelegramShop.Handlers;

[TelegramCommand("/orders", "View your order history")]
public class OrderHistoryHandler(ITelegramMessageService messaging, ShopDbContext db) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var userId = context.UserId!.Value;
        var orders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId && o.TelegramChargeId != null)
            .OrderByDescending(o => o.PaidAt)
            .Take(10)
            .ToListAsync(ct);

        if (orders.Count == 0)
        {
            await messaging.CreateMessage(context.BotId)
                .ToChat(context.ChatId!.Value)
                .WithHtml("📋 You have no orders yet.\n\nUse /catalog to start shopping!")
                .SendAsync(ct);

            return CommandResult.Ok();
        }

        var lines = orders.Select(o =>
        {
            var total = o.Items.Sum(i => i.PriceStars * i.Quantity);
            var items = string.Join(", ", o.Items.Select(i =>
                i.Quantity > 1 ? $"{i.ProductName} x{i.Quantity}" : i.ProductName));
            var date = o.PaidAt?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "pending";
            return $"🧾 <b>Order #{o.Id}</b> — {date}\n   {items}\n   Total: ⭐{total}";
        });

        var text = $"📋 <b>Order History</b>\n\n{string.Join("\n\n", lines)}";

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(text)
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}
