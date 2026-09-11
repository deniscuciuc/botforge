using TeleForge.Core;
using Telegram.Bot.Types;

namespace TeleForge.Routing.Handlers;

public interface ILocationHandler
{
    Task HandleAsync(LocationContext context, CancellationToken ct);
}

public class LocationContext(TelegramUpdateContext updateContext, Location location)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public Location Location { get; } = location;
    public double Latitude => Location.Latitude;
    public double Longitude => Location.Longitude;
    public long? ChatId => UpdateContext.ChatId;
    public long? UserId => UpdateContext.UserId;
    public string BotId => UpdateContext.BotId;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;
}
