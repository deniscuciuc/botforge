using BotForge.Core;

namespace BotForge.Routing.Handlers;

public class CommandContext(TelegramUpdateContext updateContext, string command, string[] arguments, string rawText)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public string Command { get; } = command;
    public string[] Arguments { get; } = arguments;
    public string RawText { get; } = rawText;

    public long? ChatId => UpdateContext.ChatId;
    public long? UserId => UpdateContext.UserId;
    public string BotId => UpdateContext.BotId;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;
}
