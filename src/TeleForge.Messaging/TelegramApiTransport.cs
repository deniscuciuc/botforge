using Microsoft.Extensions.Logging;
using TeleForge.Core;
using TeleForge.Messaging.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleForge.Messaging;

public class TelegramApiTransport(
    ITelegramBotClientProvider botProvider,
    ILogger<TelegramApiTransport> logger,
    IMessageRenderer? messageRenderer = null)
    : ISendMiddleware
{
    public async Task<SendResult> InvokeAsync(SendContext context, TelegramSendDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);

        var msg = context.Message;
        var client = botProvider.GetClient(msg.BotId);
        var language = msg.Language ?? "ru";

        try
        {
            var textContent = msg.TextEntities != null
                ? CreateRenderedText(msg.RawText, msg.TextEntities)
                : !string.IsNullOrEmpty(msg.TemplateKey) && messageRenderer != null
                    ? messageRenderer.RenderContent(msg.TemplateKey, msg.Parameters, language)
                    : TelegramTextEntityCompiler.CompileNullable(msg.RawText, msg.ParseMode);

            // Resolve keyboard
            InlineKeyboardMarkup? inlineKeyboard = null;
            if (!string.IsNullOrEmpty(msg.TemplateKey) && messageRenderer != null)
                inlineKeyboard = messageRenderer.BuildKeyboard(
                    msg.TemplateKey,
                    msg.Parameters,
                    language,
                    msg.DynamicButtonData?.ToDictionary(
                        kvp => kvp.Key,
                        kvp => (IReadOnlyList<KeyboardItemData>)kvp.Value),
                    msg.DynamicButtonPages);

            // Fallback to raw inline keyboard
            inlineKeyboard ??= msg.InlineKeyboard;
            var replyMarkup = ResolveReplyMarkup(msg, inlineKeyboard);
            var replyParameters = ResolveReplyParameters(msg);

            var textParseMode = ResolveParseMode(msg.ParseMode, textContent);
            var linkPreviewOptions = ResolveLinkPreviewOptions(msg);

            // Edit existing message
            if (msg.EditMessageId.HasValue)
            {
                if (msg.EditKeyboardOnly)
                {
                    if (inlineKeyboard != null)
                        await client.EditMessageReplyMarkup(
                            new ChatId(msg.ChatId),
                            msg.EditMessageId.Value,
                            inlineKeyboard,
                            cancellationToken: context.CancellationToken).ConfigureAwait(false);

                    return SendResult.Ok(msg.ChatId, msg.EditMessageId.Value);
                }

                var edited = await client.EditMessageText(
                    new ChatId(msg.ChatId),
                    msg.EditMessageId.Value,
                    textContent?.Text ?? string.Empty,
                    textParseMode,
                    inlineKeyboard,
                    linkPreviewOptions,
                    textContent?.Entities,
                    msg.BusinessConnectionId,
                    context.CancellationToken).ConfigureAwait(false);

                return SendResult.Ok(msg.ChatId, edited.Id);
            }

            // Send media
            if (msg.MediaType.HasValue && (!string.IsNullOrEmpty(msg.MediaUrl) || msg.MediaAttachment != null))
                return await SendMediaAsync(
                    client,
                    msg,
                    textContent,
                    replyMarkup,
                    replyParameters,
                    context.CancellationToken).ConfigureAwait(false);

            // Send text message
            var sent = await client.SendMessage(
                new ChatId(msg.ChatId),
                textContent?.Text ?? string.Empty,
                textParseMode,
                messageThreadId: msg.ThreadId,
                replyParameters: replyParameters,
                entities: textContent?.Entities,
                replyMarkup: replyMarkup,
                linkPreviewOptions: linkPreviewOptions,
                disableNotification: msg.DisableNotification,
                protectContent: msg.ProtectContent,
                messageEffectId: msg.MessageEffectId,
                businessConnectionId: msg.BusinessConnectionId,
                allowPaidBroadcast: msg.AllowPaidBroadcast,
                cancellationToken: context.CancellationToken).ConfigureAwait(false);

            return SendResult.Ok(msg.ChatId, sent.Id);
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == 429)
        {
            var retryAfter = ex.Parameters?.RetryAfter ?? 5;
            logger.LogWarning("Rate limited by Telegram API for bot '{BotId}'. Retry after {Seconds}s",
                msg.BotId, retryAfter);
            return SendResult.RateLimited(TimeSpan.FromSeconds(retryAfter));
        }
        catch (ApiRequestException ex)
        {
            logger.LogError(ex, "Telegram API error for bot '{BotId}': {ErrorCode} {Message}",
                msg.BotId, ex.ErrorCode, ex.Message);
            return SendResult.Failed(ex.Message, ex.ErrorCode);
        }
    }

    private static async Task<SendResult> SendMediaAsync(
        ITelegramBotClient client,
        QueuedTelegramMessage msg,
        RenderedMessageText? renderedText,
        ReplyMarkup? replyMarkup,
        ReplyParameters? replyParameters,
        CancellationToken ct)
    {
        var chatId = new ChatId(msg.ChatId);
        using var mediaStream = msg.MediaAttachment != null
            ? new MemoryStream(msg.MediaAttachment.Content, writable: false)
            : null;
        InputFile inputFile = msg.MediaAttachment != null
            ? InputFile.FromStream(mediaStream!, msg.MediaAttachment.FileName)
            : new InputFileUrl(msg.MediaUrl!);
        var captionSource = msg.CaptionEntities != null
            ? CreateRenderedText(msg.MediaCaption, msg.CaptionEntities)
            : renderedText ?? TelegramTextEntityCompiler.CompileNullable(msg.MediaCaption, msg.ParseMode);
        var caption = captionSource?.Text ?? msg.MediaCaption;
        var captionEntities = captionSource?.Entities;
        var captionParseMode = ResolveParseMode(msg.ParseMode, captionSource);

        var sent = msg.MediaType!.Value switch
        {
            TelegramMediaType.Photo => await client.SendPhoto(chatId, inputFile,
                messageThreadId: msg.ThreadId, caption: caption,
                parseMode: captionParseMode, captionEntities: captionEntities, replyMarkup: replyMarkup,
                replyParameters: replyParameters,
                showCaptionAboveMedia: msg.ShowCaptionAboveMedia, disableNotification: msg.DisableNotification,
                protectContent: msg.ProtectContent, messageEffectId: msg.MessageEffectId,
                businessConnectionId: msg.BusinessConnectionId, allowPaidBroadcast: msg.AllowPaidBroadcast,
                cancellationToken: ct).ConfigureAwait(false),

            TelegramMediaType.Video => await client.SendVideo(chatId, inputFile,
                messageThreadId: msg.ThreadId, caption: caption,
                parseMode: captionParseMode, captionEntities: captionEntities, replyMarkup: replyMarkup,
                replyParameters: replyParameters,
                showCaptionAboveMedia: msg.ShowCaptionAboveMedia, disableNotification: msg.DisableNotification,
                protectContent: msg.ProtectContent, messageEffectId: msg.MessageEffectId,
                businessConnectionId: msg.BusinessConnectionId, allowPaidBroadcast: msg.AllowPaidBroadcast,
                cancellationToken: ct).ConfigureAwait(false),

            TelegramMediaType.Document => await client.SendDocument(chatId, inputFile,
                messageThreadId: msg.ThreadId, caption: caption,
                parseMode: captionParseMode, captionEntities: captionEntities, replyMarkup: replyMarkup,
                replyParameters: replyParameters,
                disableNotification: msg.DisableNotification, protectContent: msg.ProtectContent,
                messageEffectId: msg.MessageEffectId, businessConnectionId: msg.BusinessConnectionId,
                allowPaidBroadcast: msg.AllowPaidBroadcast, cancellationToken: ct).ConfigureAwait(false),

            TelegramMediaType.Audio => await client.SendAudio(chatId, inputFile,
                messageThreadId: msg.ThreadId, caption: caption,
                parseMode: captionParseMode, captionEntities: captionEntities, replyMarkup: replyMarkup,
                replyParameters: replyParameters,
                disableNotification: msg.DisableNotification, protectContent: msg.ProtectContent,
                messageEffectId: msg.MessageEffectId, businessConnectionId: msg.BusinessConnectionId,
                allowPaidBroadcast: msg.AllowPaidBroadcast, cancellationToken: ct).ConfigureAwait(false),

            TelegramMediaType.Animation => await client.SendAnimation(chatId, inputFile,
                messageThreadId: msg.ThreadId, caption: caption,
                parseMode: captionParseMode, captionEntities: captionEntities, replyMarkup: replyMarkup,
                replyParameters: replyParameters,
                showCaptionAboveMedia: msg.ShowCaptionAboveMedia, disableNotification: msg.DisableNotification,
                protectContent: msg.ProtectContent, messageEffectId: msg.MessageEffectId,
                businessConnectionId: msg.BusinessConnectionId, allowPaidBroadcast: msg.AllowPaidBroadcast,
                cancellationToken: ct).ConfigureAwait(false),

            TelegramMediaType.Sticker => await client.SendSticker(chatId, inputFile,
                messageThreadId: msg.ThreadId, replyMarkup: replyMarkup,
                replyParameters: replyParameters,
                disableNotification: msg.DisableNotification, protectContent: msg.ProtectContent,
                messageEffectId: msg.MessageEffectId, businessConnectionId: msg.BusinessConnectionId,
                allowPaidBroadcast: msg.AllowPaidBroadcast, cancellationToken: ct).ConfigureAwait(false),

            TelegramMediaType.Voice => await client.SendVoice(chatId, inputFile,
                messageThreadId: msg.ThreadId, caption: caption,
                parseMode: captionParseMode, captionEntities: captionEntities, replyMarkup: replyMarkup,
                replyParameters: replyParameters,
                disableNotification: msg.DisableNotification, protectContent: msg.ProtectContent,
                messageEffectId: msg.MessageEffectId, businessConnectionId: msg.BusinessConnectionId,
                allowPaidBroadcast: msg.AllowPaidBroadcast, cancellationToken: ct).ConfigureAwait(false),

            TelegramMediaType.VideoNote => await client.SendVideoNote(chatId, inputFile,
                messageThreadId: msg.ThreadId, replyMarkup: replyMarkup,
                replyParameters: replyParameters,
                disableNotification: msg.DisableNotification, protectContent: msg.ProtectContent,
                messageEffectId: msg.MessageEffectId, businessConnectionId: msg.BusinessConnectionId,
                allowPaidBroadcast: msg.AllowPaidBroadcast, cancellationToken: ct).ConfigureAwait(false),

            _ => throw new ArgumentOutOfRangeException(nameof(msg), msg.MediaType, "Unsupported media type."),
        };

        return SendResult.Ok(msg.ChatId, sent.Id);
    }

    private static ParseMode MapParseMode(TelegramParseMode mode)
    {
        return mode switch
        {
            TelegramParseMode.None => ParseMode.None,
            TelegramParseMode.Html => ParseMode.Html,
            TelegramParseMode.Markdown => ParseMode.Markdown,
            TelegramParseMode.MarkdownV2 => ParseMode.MarkdownV2,
            _ => ParseMode.None
        };
    }

    private static ParseMode ResolveParseMode(TelegramParseMode mode, RenderedMessageText? content)
    {
        return content is { HasEntities: true } ? ParseMode.None : MapParseMode(mode);
    }

    private static LinkPreviewOptions? ResolveLinkPreviewOptions(QueuedTelegramMessage msg)
    {
        return msg.DisableLinkPreview ? new LinkPreviewOptions { IsDisabled = true } : null;
    }

    private static ReplyParameters? ResolveReplyParameters(QueuedTelegramMessage msg)
    {
        return msg.ReplyToMessageId.HasValue
            ? new ReplyParameters { MessageId = msg.ReplyToMessageId.Value }
            : null;
    }

    private static RenderedMessageText CreateRenderedText(string? text, IReadOnlyList<MessageEntity>? entities)
    {
        return new RenderedMessageText
        {
            Text = text ?? string.Empty,
            Entities = entities?.ToArray()
        };
    }

    private static ReplyMarkup? ResolveReplyMarkup(QueuedTelegramMessage msg, InlineKeyboardMarkup? inlineKeyboard)
    {
        if (msg.RemoveReplyKeyboard)
            return new ReplyKeyboardRemove();

        if (msg.ForceReply != null)
            return new ForceReplyMarkup
            {
                InputFieldPlaceholder = msg.ForceReply.InputFieldPlaceholder,
                Selective = msg.ForceReply.Selective
            };

        if (msg.ReplyKeyboard == null) return inlineKeyboard;

        var rows = msg.ReplyKeyboard.Buttons
            .Select(row => row.Select(CreateReplyKeyboardButton).ToArray())
            .ToArray();

        return new ReplyKeyboardMarkup(rows)
        {
            ResizeKeyboard = msg.ReplyKeyboard.ResizeKeyboard,
            OneTimeKeyboard = msg.ReplyKeyboard.OneTimeKeyboard,
            InputFieldPlaceholder = msg.ReplyKeyboard.InputFieldPlaceholder,
            IsPersistent = msg.ReplyKeyboard.IsPersistent
        };
    }

    private static KeyboardButton CreateReplyKeyboardButton(ReplyKeyboardButtonData button)
    {
        var telegramButton = button.Type switch
        {
            TelegramReplyButtonType.Text => new KeyboardButton(button.Text),
            TelegramReplyButtonType.RequestContact => KeyboardButton.WithRequestContact(button.Text),
            TelegramReplyButtonType.RequestLocation => KeyboardButton.WithRequestLocation(button.Text),
            TelegramReplyButtonType.RequestPoll => KeyboardButton.WithRequestPoll(
                button.Text,
                button.RequestPoll ?? new KeyboardButtonPollType()),
            TelegramReplyButtonType.RequestUsers => button.RequestUsers != null
                ? KeyboardButton.WithRequestUsers(button.Text, button.RequestUsers)
                : throw new InvalidOperationException("Reply keyboard button requires RequestUsers metadata."),
            TelegramReplyButtonType.RequestChat => button.RequestChat != null
                ? KeyboardButton.WithRequestChat(button.Text, button.RequestChat)
                : throw new InvalidOperationException("Reply keyboard button requires RequestChat metadata."),
            TelegramReplyButtonType.WebApp => button.WebApp != null
                ? KeyboardButton.WithWebApp(button.Text, button.WebApp)
                : throw new InvalidOperationException("Reply keyboard button requires WebApp metadata."),
            _ => throw new ArgumentOutOfRangeException(nameof(button), button.Type, "Unsupported reply button type.")
        };

        telegramButton.Style = MapButtonStyle(button.Style);
        telegramButton.IconCustomEmojiId = button.IconCustomEmojiId;
        return telegramButton;
    }

    private static KeyboardButtonStyle? MapButtonStyle(TelegramButtonStyle? style)
    {
        return style switch
        {
            TelegramButtonStyle.Primary => KeyboardButtonStyle.Primary,
            TelegramButtonStyle.Success => KeyboardButtonStyle.Success,
            TelegramButtonStyle.Danger => KeyboardButtonStyle.Danger,
            _ => null
        };
    }
}
