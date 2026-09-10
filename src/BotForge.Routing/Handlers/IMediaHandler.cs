using BotForge.Core;
using Telegram.Bot.Types;

namespace BotForge.Routing.Handlers;

public enum MediaType
{
    Photo,
    Video,
    Audio,
    Document,
    Voice,
    VideoNote,
    Sticker,
    Animation
}

public interface IMediaHandler
{
    Task HandleAsync(MediaContext context, CancellationToken ct);
}

public class MediaContext(TelegramUpdateContext updateContext, Message message, MediaType mediaType)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public Message Message { get; } = message;
    public MediaType MediaType { get; } = mediaType;
    public long? ChatId => UpdateContext.ChatId;
    public long? UserId => UpdateContext.UserId;
    public string BotId => UpdateContext.BotId;
    public string? Caption => Message.Caption;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;
}
