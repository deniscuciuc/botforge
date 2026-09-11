using TeleForge.Core;

namespace TeleForge.Routing.Handlers;

/// <summary>
/// Handler for plain text messages (non-command).
/// </summary>
public interface ITextMessageHandler
{
    Task<TextMessageResult> HandleAsync(TextMessageContext context, CancellationToken ct);
}

/// <summary>
/// Context for text message handling.
/// </summary>
public class TextMessageContext(TelegramUpdateContext updateContext, string text)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public string Text { get; } = text;

    public long? ChatId => UpdateContext.ChatId;
    public long? UserId => UpdateContext.UserId;
    public string BotId => UpdateContext.BotId;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;
}

/// <summary>
/// Result of a text message handler.
/// </summary>
public class TextMessageResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static TextMessageResult Ok()
    {
        return new TextMessageResult { Success = true };
    }

    public static TextMessageResult Fail(string error)
    {
        return new TextMessageResult { Success = false, ErrorMessage = error };
    }
}
