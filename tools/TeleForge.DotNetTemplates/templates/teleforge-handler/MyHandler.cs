using TeleForge.Core;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Handlers;

// TODO: Update the attribute parameters and implement handler logic
[TelegramCommand("/myhandler", Description = "My handler description")]
public class MyHandler : ICommandHandler
{
    public Task<HandlerResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        // TODO: Implement handler logic
        return Task.FromResult(HandlerResult.Ok());
    }
}
