using TeleForge.Core;

namespace TeleForge.Routing.Handlers;

public class CallbackContext(
    TelegramUpdateContext updateContext,
    string callbackData,
    string callbackQueryId,
    IDictionary<string, string> routeParameters)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public string CallbackData { get; } = callbackData;
    public string CallbackQueryId { get; } = callbackQueryId;
    public IDictionary<string, string> RouteParameters { get; } = routeParameters;

    public long? ChatId => UpdateContext.ChatId;
    public long? UserId => UpdateContext.UserId;
    public string BotId => UpdateContext.BotId;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;

    public T GetRouteParam<T>(string name) where T : IParsable<T>
    {
        return !RouteParameters.TryGetValue(name, out var value)
            ? throw new KeyNotFoundException($"Route parameter '{name}' not found in callback data.")
            : T.Parse(value, null);
    }

    public string GetRouteParam(string name)
    {
        return !RouteParameters.TryGetValue(name, out var value)
            ? throw new KeyNotFoundException($"Route parameter '{name}' not found in callback data.")
            : value;
    }
}
