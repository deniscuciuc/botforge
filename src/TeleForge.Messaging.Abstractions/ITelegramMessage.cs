using TeleForge.Core;

namespace TeleForge.Messaging.Abstractions;

public interface ITelegramMessage
{
    // Target
    ITelegramMessage ToChat(long chatId);
    ITelegramMessage InThread(int threadId);
    ITelegramMessage ReplyToMessage(int messageId);

    // Content via template
    ITelegramMessage WithTemplate(string templateName);
    ITelegramMessage WithParameters(IDictionary<string, string> parameters);
    ITelegramMessage WithParameter(string key, string value);
    ITelegramMessage WithLanguage(string language);

    // Content via direct text
    ITelegramMessage WithText(string text);
    ITelegramMessage WithHtml(string html);
    ITelegramMessage WithTextEntities(string text, IReadOnlyList<global::Telegram.Bot.Types.MessageEntity> entities);
    ITelegramMessage WithParseMode(TelegramParseMode parseMode);

    // Media
    ITelegramMessage WithMedia(TelegramMediaType type, string url, string? caption = null);
    ITelegramMessage WithMedia(TelegramMediaType type, byte[] content, string fileName, string? caption = null);

    ITelegramMessage WithCaptionEntities(string caption,
        IReadOnlyList<global::Telegram.Bot.Types.MessageEntity> entities);

    // Dynamic content (keyboard items for template dynamic buttons)
    ITelegramMessage WithKeyboardItems(string slotName, IReadOnlyList<KeyboardItemData> items, int page = 1);

    // Inline keyboard (raw markup, bypasses template keyboard builder)
    ITelegramMessage WithInlineKeyboard(global::Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup keyboard);

    // Edit mode
    ITelegramMessage EditMessage(int messageId);
    ITelegramMessage EditKeyboardOnly(int messageId);

    // Reply keyboard
    ITelegramMessage WithReplyKeyboard(IEnumerable<IEnumerable<string>> rows, bool resize = true, bool oneTime = false,
        string? placeholder = null, bool isPersistent = false);

    ITelegramMessage WithReplyKeyboard(IEnumerable<IEnumerable<ReplyKeyboardButtonData>> rows, bool resize = true,
        bool oneTime = false, string? placeholder = null, bool isPersistent = false);

    ITelegramMessage WithForceReply(string? placeholder = null);
    ITelegramMessage RemoveReplyKeyboard();

    // Delivery options
    ITelegramMessage WithPriority(MessagePriority priority);
    ITelegramMessage BypassRateLimit();
    ITelegramMessage DisableNotification();
    ITelegramMessage DisableLinkPreview();
    ITelegramMessage ProtectContent();
    ITelegramMessage ShowCaptionAboveMedia();
    ITelegramMessage AllowPaidBroadcast();
    ITelegramMessage WithMessageEffect(string messageEffectId);
    ITelegramMessage WithBusinessConnection(string businessConnectionId);
    ITelegramMessage ScheduleAt(DateTimeOffset sendAt);

    // Send
    Task<SendResult> SendAsync(CancellationToken ct = default);
    Task<SendResult> QueueAsync(CancellationToken ct = default);
    Task<SendResult> SendOrQueueAsync(CancellationToken ct = default);

    // Build for external use
    QueuedTelegramMessage Build();
}
