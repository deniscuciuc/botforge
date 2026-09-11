using TeleForge.Messaging.Abstractions;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Handlers;

namespace WebhookBot.Handlers;

[TelegramCommand("/start", "Start the webhook bot")]
public class StartHandler(ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "<b>Webhook Bot</b>\n\n" +
                "This bot runs via webhooks instead of long polling.\n\n" +
                "Commands:\n" +
                "/ping — Check if the bot is alive\n" +
                "/info — Show bot information")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

[TelegramCommand("/ping", "Check bot responsiveness")]
public class PingHandler(ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("🏓 Pong! Bot is running via webhook.")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

[TelegramCommand("/info", "Show bot and server info")]
public class InfoHandler(ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var info =
            "<b>Bot Information</b>\n\n" +
            $"🔗 Transport: Webhook\n" +
            $"🕐 Server Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
            $"🖥️ Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}\n" +
            $"📦 .NET: {Environment.Version}";

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(info)
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

[TextMessage]
public class EchoHandler(ITelegramMessageService messaging) : ITextMessageHandler
{
    public async Task<TextMessageResult> HandleAsync(TextMessageContext context, CancellationToken ct)
    {
        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText($"📨 Received via webhook: {context.Text}")
            .SendAsync(ct);

        return TextMessageResult.Ok();
    }
}
