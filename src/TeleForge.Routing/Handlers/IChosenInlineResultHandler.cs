using TeleForge.Core;
using Telegram.Bot.Types;

namespace TeleForge.Routing.Handlers;

public interface IChosenInlineResultHandler
{
    Task HandleAsync(ChosenInlineResultContext context, CancellationToken ct);
}

public class ChosenInlineResultContext(TelegramUpdateContext updateContext, ChosenInlineResult chosenResult)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public ChosenInlineResult ChosenResult { get; } = chosenResult;
    public string ResultId => ChosenResult.ResultId;
    public string Query => ChosenResult.Query;
    public long UserId => ChosenResult.From.Id;
    public string BotId => UpdateContext.BotId;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;
}
