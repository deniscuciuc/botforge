using Microsoft.Extensions.Logging;
using TeleForge.Core;
using TeleForge.Payments.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleForge.Payments.PaidMedia;

public class PaidMediaService(
    ITelegramBotClientProvider botProvider,
    IPaymentMetrics metrics,
    ILogger<PaidMediaService> logger)
    : IPaidMediaService
{
    public async Task<Message> SendPaidMediaAsync(PaidMediaDefinition definition, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var client = botProvider.GetClient(definition.BotId);
        var parseMode = definition.ParseMode switch
        {
            "Html" or "html" => ParseMode.Html,
            "Markdown" or "markdown" => ParseMode.Markdown,
            "MarkdownV2" or "markdownv2" => ParseMode.MarkdownV2,
            _ => ParseMode.None
        };

        var message = await client.SendPaidMedia(
            definition.ChatId,
            definition.StarCount,
            definition.Media,
            definition.Caption,
            parseMode,
            showCaptionAboveMedia: definition.ShowCaptionAboveMedia ?? false,
            disableNotification: definition.DisableNotification,
            protectContent: definition.ProtectContent,
            replyParameters: definition.ReplyToMessageId.HasValue
                ? new ReplyParameters { MessageId = definition.ReplyToMessageId.Value }
                : null,
            businessConnectionId: definition.BusinessConnectionId,
            payload: definition.Payload,
            cancellationToken: ct).ConfigureAwait(false);

        metrics.PaidMediaSold(definition.BotId, definition.StarCount);
        logger.LogInformation(
            "Paid media sent to chat {ChatId}, price {StarCount} Stars, {MediaCount} items",
            definition.ChatId, definition.StarCount, definition.Media.Count);

        return message;
    }
}
