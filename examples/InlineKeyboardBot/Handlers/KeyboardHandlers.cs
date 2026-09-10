using BotForge.Messaging.Abstractions;
using BotForge.Routing.Attributes;
using BotForge.Routing.Handlers;

namespace InlineKeyboardBot.Handlers;

[TelegramCommand("/start", "Show main menu")]
public class MenuHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithTemplate("main_menu")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[CallbackQuery("menu:categories")]
public class CategoriesHandler(ITelegramMessageService messages) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var messageId = context.UpdateContext.RawUpdate.CallbackQuery?.Message?.MessageId;
        var builder = messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml("📋 <b>Categories</b>\n\nChoose a category to explore:");

        if (messageId.HasValue)
            builder.EditMessage(messageId.Value);

        await builder.SendAsync(ct);
        return CallbackResult.Ok();
    }
}

[CallbackQuery("menu:about")]
public class AboutHandler(ITelegramMessageService messages) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var messageId = context.UpdateContext.RawUpdate.CallbackQuery?.Message?.MessageId;
        var builder = messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "ℹ️ <b>About</b>\n\n" +
                "This bot demonstrates inline keyboard navigation.\n" +
                "Press Back to return to the main menu.");

        if (messageId.HasValue)
            builder.EditMessage(messageId.Value);

        await builder.SendAsync(ct);
        return CallbackResult.Ok();
    }
}

[CallbackQuery("menu:settings")]
public class SettingsHandler(ITelegramMessageService messages) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var messageId = context.UpdateContext.RawUpdate.CallbackQuery?.Message?.MessageId;
        var builder = messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "⚙️ <b>Settings</b>\n\n" +
                "🔔 Notifications: ON\n" +
                "🌍 Language: English");

        if (messageId.HasValue)
            builder.EditMessage(messageId.Value);

        await builder.SendAsync(ct);
        return CallbackResult.Ok();
    }
}

[CallbackQuery("back:main")]
public class BackToMainHandler(ITelegramMessageService messages) : ICallbackQueryHandler
{
    public async Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct)
    {
        var messageId = context.UpdateContext.RawUpdate.CallbackQuery?.Message?.MessageId;
        var builder = messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithTemplate("main_menu");

        if (messageId.HasValue)
            builder.EditMessage(messageId.Value);

        await builder.SendAsync(ct);
        return CallbackResult.Ok();
    }
}
