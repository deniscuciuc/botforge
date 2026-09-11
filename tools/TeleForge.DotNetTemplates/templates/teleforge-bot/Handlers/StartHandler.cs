using TeleForge.Core;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Handlers;

namespace TeleForgeBot.Handlers;

[TelegramCommand("/start", Description = "Start the bot")]
public class StartHandler : ICommandHandler
{
    public async Task<HandlerResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var messaging = context.RequestServices.GetRequiredService<TeleForge.Messaging.Abstractions.ITelegramMessageService>();
        await messaging.Create("main")
            .ToChat(context.ChatId)
            .WithText("👋 Welcome! I'm your TeleForge bot. Use /help to see available commands.")
            .SendAsync(ct);

        return HandlerResult.Ok();
    }
}
