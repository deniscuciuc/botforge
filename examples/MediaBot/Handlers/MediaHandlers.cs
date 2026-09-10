using BotForge.Messaging.Abstractions;
using BotForge.Routing.Attributes;
using BotForge.Routing.Handlers;

namespace MediaBot.Handlers;

[TelegramCommand("/start", "Show welcome message")]
public class StartHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "📸 <b>MediaBot</b>\n\n" +
                "Send me any media and I'll tell you about it:\n" +
                "• Photos\n" +
                "• Documents\n" +
                "• Voice messages\n" +
                "• Videos\n" +
                "• Stickers\n" +
                "• Animations (GIFs)")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[MediaMessage(MediaType.Photo)]
public class PhotoHandler(ITelegramMessageService messages) : IMediaHandler
{
    public async Task HandleAsync(MediaContext context, CancellationToken ct)
    {
        var fileId = context.Message.Photo?.LastOrDefault()?.FileId ?? "N/A";
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "📷 <b>Photo received!</b>\n\n" +
                $"File ID: <code>{fileId}</code>\n" +
                $"Has caption: {(!string.IsNullOrEmpty(context.Caption) ? "Yes" : "No")}")
            .SendAsync(ct);
    }
}

[MediaMessage(MediaType.Document)]
public class DocumentHandler(ITelegramMessageService messages) : IMediaHandler
{
    public async Task HandleAsync(MediaContext context, CancellationToken ct)
    {
        var doc = context.Message.Document;
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "📄 <b>Document received!</b>\n\n" +
                $"File ID: <code>{doc?.FileId ?? "N/A"}</code>\n" +
                $"File name: {doc?.FileName ?? "Unknown"}")
            .SendAsync(ct);
    }
}

[MediaMessage(MediaType.Voice)]
public class VoiceHandler(ITelegramMessageService messages) : IMediaHandler
{
    public async Task HandleAsync(MediaContext context, CancellationToken ct)
    {
        var voice = context.Message.Voice;
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "🎤 <b>Voice message received!</b>\n\n" +
                $"Duration: {voice?.Duration ?? 0}s\n" +
                $"File ID: <code>{voice?.FileId ?? "N/A"}</code>")
            .SendAsync(ct);
    }
}

[MediaMessage(MediaType.Video)]
public class VideoHandler(ITelegramMessageService messages) : IMediaHandler
{
    public async Task HandleAsync(MediaContext context, CancellationToken ct)
    {
        var video = context.Message.Video;
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "🎬 <b>Video received!</b>\n\n" +
                $"Duration: {video?.Duration ?? 0}s\n" +
                $"File ID: <code>{video?.FileId ?? "N/A"}</code>")
            .SendAsync(ct);
    }
}

[MediaMessage(MediaType.Sticker)]
public class StickerHandler(ITelegramMessageService messages) : IMediaHandler
{
    public async Task HandleAsync(MediaContext context, CancellationToken ct)
    {
        var emoji = context.Message.Sticker?.Emoji ?? "?";
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText($"🏷️ Sticker received! Emoji: {emoji}")
            .SendAsync(ct);
    }
}

[MediaMessage(MediaType.Animation)]
public class AnimationHandler(ITelegramMessageService messages) : IMediaHandler
{
    public async Task HandleAsync(MediaContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("🎞️ GIF/Animation received!")
            .SendAsync(ct);
    }
}
