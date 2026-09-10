using BotForge.Core;
using BotForge.Routing.Attributes;
using BotForge.Routing.Handlers;

namespace BotForgeBot.Handlers;

[TelegramCommand("/start", Description = "Start the bot")]
public class StartHandler : ICommandHandler
{
    public async Task<HandlerResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var messaging = context.RequestServices.GetRequiredService<BotForge.Messaging.Abstractions.ITelegramMessageService>();
        await messaging.Create("main")
            .ToChat(context.ChatId)
            .WithText("👋 Welcome! I'm your BotForge bot. Use /help to see available commands.")
            .SendAsync(ct);

        return HandlerResult.Ok();
    }
}
