namespace BotForge.Core;

public class QueuedTelegramMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string BotId { get; init; } = null!;
    public long ChatId { get; init; }
    public int? ThreadId { get; init; }
    public int? ReplyToMessageId { get; init; }
    public string? TemplateKey { get; init; }
    public string? RawText { get; init; }
    public TelegramParseMode ParseMode { get; init; } = TelegramParseMode.Html;
    public IReadOnlyList<global::Telegram.Bot.Types.MessageEntity>? TextEntities { get; init; }
    public Dictionary<string, string> Parameters { get; init; } = new();
    public string? Language { get; init; }
    public string? SerializedKeyboard { get; init; }
    public int? EditMessageId { get; init; }
    public bool EditKeyboardOnly { get; init; }
    public MessagePriority Priority { get; init; } = MessagePriority.Normal;
    public bool DisableNotification { get; init; }
    public bool ProtectContent { get; init; }
    public bool DisableLinkPreview { get; init; }
    public bool ShowCaptionAboveMedia { get; init; }
    public bool AllowPaidBroadcast { get; init; }
    public string? MessageEffectId { get; init; }
    public string? BusinessConnectionId { get; init; }
    public DateTimeOffset? ScheduledAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public TelegramMediaType? MediaType { get; init; }
    public string? MediaUrl { get; init; }
    public TelegramMediaAttachment? MediaAttachment { get; init; }
    public string? MediaCaption { get; init; }
    public IReadOnlyList<global::Telegram.Bot.Types.MessageEntity>? CaptionEntities { get; init; }

    public Dictionary<string, List<KeyboardItemData>>? DynamicButtonData { get; init; }
    public Dictionary<string, int>? DynamicButtonPages { get; init; }

    // Reply keyboard support
    public ReplyKeyboardData? ReplyKeyboard { get; init; }
    public bool RemoveReplyKeyboard { get; init; }
    public ForceReplyData? ForceReply { get; init; }

    // Raw inline keyboard (bypasses template system)
    public global::Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup? InlineKeyboard { get; init; }
}

public class ReplyKeyboardData
{
    public List<List<ReplyKeyboardButtonData>> Buttons { get; init; } = [];
    public bool ResizeKeyboard { get; init; } = true;
    public bool OneTimeKeyboard { get; init; }
    public string? InputFieldPlaceholder { get; init; }
    public bool IsPersistent { get; init; }
}

public class ReplyKeyboardButtonData
{
    public string Text { get; init; } = null!;
    public TelegramReplyButtonType Type { get; init; } = TelegramReplyButtonType.Text;
    public TelegramButtonStyle? Style { get; init; }
    public string? IconCustomEmojiId { get; init; }
    public global::Telegram.Bot.Types.ReplyMarkups.KeyboardButtonPollType? RequestPoll { get; init; }
    public global::Telegram.Bot.Types.ReplyMarkups.KeyboardButtonRequestUsers? RequestUsers { get; init; }
    public global::Telegram.Bot.Types.ReplyMarkups.KeyboardButtonRequestChat? RequestChat { get; init; }
    public global::Telegram.Bot.Types.WebAppInfo? WebApp { get; init; }
}

public class ForceReplyData
{
    public string? InputFieldPlaceholder { get; init; }
    public bool Selective { get; init; }
}

public class TelegramMediaAttachment
{
    public string FileName { get; init; } = null!;
    public byte[] Content { get; init; } = null!;
}
