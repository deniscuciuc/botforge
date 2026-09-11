namespace TeleForge.Routing.Handlers;

public interface ICallbackQueryHandler
{
    Task<CallbackResult> HandleAsync(CallbackContext context, CancellationToken ct);
}
