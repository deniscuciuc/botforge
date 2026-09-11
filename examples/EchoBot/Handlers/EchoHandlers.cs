using TeleForge.Messaging.Abstractions;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Handlers;

namespace EchoBot.Handlers;

[TelegramCommand("/start")]
public class StartHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("👋 Hi! I'm an echo bot. Send me any message and I'll repeat it back to you!")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TextMessage]
public class EchoHandler(ITelegramMessageService messages) : ITextMessageHandler
{
    public async Task<TextMessageResult> HandleAsync(TextMessageContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText($"🔁 {context.Text}")
            .SendAsync(ct);
        return TextMessageResult.Ok();
    }
}
