using TeleForge.Core;
using Telegram.Bot.Types;

namespace TeleForge.Routing.Handlers;

public interface IInlineQueryHandler
{
    Task HandleAsync(InlineQueryContext context, CancellationToken ct);
}

public class InlineQueryContext(TelegramUpdateContext updateContext, InlineQuery inlineQuery)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public InlineQuery InlineQuery { get; } = inlineQuery;
    public string Query { get; } = inlineQuery.Query;
    public long UserId => InlineQuery.From.Id;
    public string BotId => UpdateContext.BotId;
    public string? Offset => InlineQuery.Offset;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;
}
