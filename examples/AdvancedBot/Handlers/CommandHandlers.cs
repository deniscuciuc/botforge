using TeleForge.Messaging.Abstractions;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Handlers;

namespace AdvancedBot.Handlers;

[TelegramCommand("/start", "Welcome message")]
public class StartHandler(ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "<b>Advanced Bot</b>\n\n" +
                "Features:\n" +
                "• Multi-bot (main + notifications)\n" +
                "• Redis rate limiting\n" +
                "• MassTransit message queue\n" +
                "• Authorization policies\n\n" +
                "Commands:\n" +
                "/notify — Send via notification bot\n" +
                "/admin — Admin-only command\n" +
                "/moderate — Moderator command\n" +
                "/status — Bot status")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

/// <summary>
/// Sends a message via the notification bot instead of the main bot.
/// Demonstrates multi-bot messaging.
/// </summary>
[TelegramCommand("/notify", "Send via notification bot")]
public class NotifyHandler(ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        // Send reply on the main bot
        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("📤 Sending a notification via the notification bot...")
            .SendAsync(ct);

        // Send notification via the separate notification bot
        await messaging.CreateMessage("notifications")
            .ToChat(context.ChatId!.Value)
            .WithHtml("🔔 <b>Notification</b>\n\nThis message was sent by the notification bot!")
            .QueueAsync(ct);

        return CommandResult.Ok();
    }
}

/// <summary>
/// Admin-only command protected by the "admin" authorization policy.
/// </summary>
[TelegramCommand("/admin", "Admin-only command")]
[Authorize("admin")]
public class AdminHandler(ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "🔐 <b>Admin Panel</b>\n\nYou have admin access. This command is protected by the <code>admin</code> policy.")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

/// <summary>
/// Moderator command requiring moderator or admin rank.
/// </summary>
[TelegramCommand("/moderate", "Moderator command")]
[Authorize("moderator")]
public class ModerateHandler(ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml("🛡️ <b>Moderation Panel</b>\n\nYou have moderator access.")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

/// <summary>
/// Rate-limited status command — can only be called once every 10 seconds per user.
/// </summary>
[TelegramCommand("/status", "Bot status")]
[RateLimit(10)]
public class StatusHandler(ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "<b>Bot Status</b>\n\n" +
                $"🕐 Server Time: {DateTime.UtcNow:HH:mm:ss} UTC\n" +
                $"📦 .NET: {Environment.Version}\n" +
                $"🤖 Bot: {context.BotId}\n" +
                $"💾 Queue: MassTransit + RabbitMQ\n" +
                $"⚡ Rate Limit: Redis")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

/// <summary>
/// Private-only command that only works in direct messages.
/// </summary>
[TelegramCommand("/private", "Private chat only")]
[Authorize("private-only")]
public class PrivateHandler(ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("🔒 This command only works in private chats.")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}
