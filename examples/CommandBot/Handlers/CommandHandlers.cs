using TeleForge.Messaging.Abstractions;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Handlers;

namespace CommandBot.Handlers;

[TelegramCommand("/start", "Show welcome message")]
public class StartHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "👋 <b>Welcome to CommandBot!</b>\n\n" +
                "Available commands:\n" +
                "/start — Show this welcome message\n" +
                "/help — Get help information\n" +
                "/settings — View your settings\n" +
                "/about — About this bot")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/help", "Get help information")]
public class HelpHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "ℹ️ <b>Help</b>\n\n" +
                "This bot demonstrates command routing.\n" +
                "Each command is handled by a separate class decorated with " +
                "<code>[TelegramCommand]</code> attribute.\n\n" +
                "Try sending any of the available commands!")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/settings", "View your settings")]
public class SettingsHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var text = context.Arguments.Length == 0
            ? "⚙️ <b>Settings</b>\n\nNo settings configured yet.\nUsage: /settings &lt;key&gt; &lt;value&gt;"
            : $"⚙️ Setting updated: <code>{string.Join(" ", context.Arguments)}</code>";

        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(text)
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/about", "About this bot")]
public class AboutHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "🤖 <b>CommandBot</b>\n\n" +
                "Built with TeleForge Framework\n" +
                "Demonstrates command routing with attributes")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}
