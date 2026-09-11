namespace TeleForge.Routing.Handlers;

public interface ICommandHandler
{
    Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct);
}
