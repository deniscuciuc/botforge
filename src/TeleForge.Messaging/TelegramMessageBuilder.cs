using TeleForge.Core;
using TeleForge.Messaging.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleForge.Messaging;

public class TelegramMessageBuilder(
    string botId,
    ISendPipeline sendPipeline,
    IMessageQueueBackend? queueBackend,
    ITelegramBotClientProvider botProvider) : ITelegramMessage
{
    private long _chatId;
    private int? _threadId;
    private int? _replyToMessageId;
    private string? _templateName;
    private string? _rawText;
    private TelegramParseMode _parseMode = TelegramParseMode.Html;
    private IReadOnlyList<MessageEntity>? _textEntities;
    private string? _language;
    private readonly Dictionary<string, string> _parameters = new();
    private readonly Dictionary<string, IReadOnlyList<KeyboardItemData>> _dynamicData = new();
    private readonly Dictionary<string, int> _dynamicPages = new();
    private int? _editMessageId;
    private bool _editKeyboardOnly;
    private MessagePriority _priority = MessagePriority.Normal;
    private bool _bypassRateLimit;
    private bool _disableNotification;
    private bool _protectContent;
    private bool _disableLinkPreview;
    private bool _showCaptionAboveMedia;
    private bool _allowPaidBroadcast;
    private string? _messageEffectId;
    private string? _businessConnectionId;
    private DateTimeOffset? _scheduledAt;
    private TelegramMediaType? _mediaType;
    private string? _mediaUrl;
    private TelegramMediaAttachment? _mediaAttachment;
    private string? _mediaCaption;
    private IReadOnlyList<MessageEntity>? _captionEntities;
    private ReplyKeyboardData? _replyKeyboard;
    private bool _removeReplyKeyboard;
    private ForceReplyData? _forceReply;
    private InlineKeyboardMarkup? _inlineKeyboard;

    public ITelegramMessage ToChat(long chatId)
    {
        _chatId = chatId;
        return this;
    }

    public ITelegramMessage InThread(int threadId)
    {
        _threadId = threadId;
        return this;
    }

    public ITelegramMessage ReplyToMessage(int messageId)
    {
        _replyToMessageId = messageId;
        return this;
    }

    public ITelegramMessage WithTemplate(string templateName)
    {
        _templateName = templateName;
        return this;
    }

    public ITelegramMessage WithParameters(IDictionary<string, string> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        foreach (var kvp in parameters)
            _parameters[kvp.Key] = kvp.Value;
        return this;
    }

    public ITelegramMessage WithParameter(string key, string value)
    {
        _parameters[key] = value;
        return this;
    }

    public ITelegramMessage WithLanguage(string language)
    {
        _language = language;
        return this;
    }

    public ITelegramMessage WithText(string text)
    {
        _rawText = text;
        _textEntities = null;
        return this;
    }

    public ITelegramMessage WithHtml(string html)
    {
        _rawText = html;
        _parseMode = TelegramParseMode.Html;
        _textEntities = null;
        return this;
    }

    public ITelegramMessage WithTextEntities(string text, IReadOnlyList<MessageEntity> entities)
    {
        _rawText = text;
        _textEntities = entities.ToArray();
        return this;
    }

    public ITelegramMessage WithParseMode(TelegramParseMode parseMode)
    {
        _parseMode = parseMode;
        return this;
    }

    public ITelegramMessage WithMedia(TelegramMediaType type, string url, string? caption = null)
    {
        _mediaType = type;
        _mediaUrl = url;
        _mediaAttachment = null;
        _mediaCaption = caption;
        _captionEntities = null;
        return this;
    }

    public ITelegramMessage WithMedia(TelegramMediaType type, byte[] content, string fileName, string? caption = null)
    {
        _mediaType = type;
        _mediaUrl = null;
        _mediaAttachment = new TelegramMediaAttachment
        {
            FileName = fileName,
            Content = content.ToArray()
        };
        _mediaCaption = caption;
        _captionEntities = null;
        return this;
    }

    public ITelegramMessage WithCaptionEntities(string caption, IReadOnlyList<MessageEntity> entities)
    {
        _mediaCaption = caption;
        _captionEntities = entities.ToArray();
        return this;
    }

    public ITelegramMessage WithKeyboardItems(string slotName, IReadOnlyList<KeyboardItemData> items, int page = 1)
    {
        _dynamicData[slotName] = items;
        _dynamicPages[slotName] = page;
        return this;
    }

    public ITelegramMessage WithInlineKeyboard(InlineKeyboardMarkup keyboard)
    {
        _inlineKeyboard = keyboard;
        return this;
    }

    public ITelegramMessage EditMessage(int messageId)
    {
        _editMessageId = messageId;
        return this;
    }

    public ITelegramMessage EditKeyboardOnly(int messageId)
    {
        _editMessageId = messageId;
        _editKeyboardOnly = true;
        return this;
    }

    public ITelegramMessage WithReplyKeyboard(IEnumerable<IEnumerable<string>> rows, bool resize = true,
        bool oneTime = false, string? placeholder = null, bool isPersistent = false)
    {
        _replyKeyboard = new ReplyKeyboardData
        {
            Buttons = rows
                .Select(row => row
                    .Select(text => new ReplyKeyboardButtonData { Text = text })
                    .ToList())
                .ToList(),
            ResizeKeyboard = resize,
            OneTimeKeyboard = oneTime,
            InputFieldPlaceholder = placeholder,
            IsPersistent = isPersistent
        };
        return this;
    }

    public ITelegramMessage WithReplyKeyboard(IEnumerable<IEnumerable<ReplyKeyboardButtonData>> rows,
        bool resize = true,
        bool oneTime = false, string? placeholder = null, bool isPersistent = false)
    {
        _replyKeyboard = new ReplyKeyboardData
        {
            Buttons = rows.Select(row => row.ToList()).ToList(),
            ResizeKeyboard = resize,
            OneTimeKeyboard = oneTime,
            InputFieldPlaceholder = placeholder,
            IsPersistent = isPersistent
        };
        return this;
    }

    public ITelegramMessage WithForceReply(string? placeholder = null)
    {
        _forceReply = new ForceReplyData { InputFieldPlaceholder = placeholder };
        return this;
    }

    public ITelegramMessage RemoveReplyKeyboard()
    {
        _removeReplyKeyboard = true;
        return this;
    }

    public ITelegramMessage WithPriority(MessagePriority priority)
    {
        _priority = priority;
        return this;
    }

    public ITelegramMessage BypassRateLimit()
    {
        _bypassRateLimit = true;
        return this;
    }

    public ITelegramMessage DisableNotification()
    {
        _disableNotification = true;
        return this;
    }

    public ITelegramMessage ProtectContent()
    {
        _protectContent = true;
        return this;
    }

    public ITelegramMessage DisableLinkPreview()
    {
        _disableLinkPreview = true;
        return this;
    }

    public ITelegramMessage ShowCaptionAboveMedia()
    {
        _showCaptionAboveMedia = true;
        return this;
    }

    public ITelegramMessage AllowPaidBroadcast()
    {
        _allowPaidBroadcast = true;
        return this;
    }

    public ITelegramMessage WithMessageEffect(string messageEffectId)
    {
        _messageEffectId = messageEffectId;
        return this;
    }

    public ITelegramMessage WithBusinessConnection(string businessConnectionId)
    {
        _businessConnectionId = businessConnectionId;
        return this;
    }

    public ITelegramMessage ScheduleAt(DateTimeOffset sendAt)
    {
        _scheduledAt = sendAt;
        return this;
    }

    public QueuedTelegramMessage Build()
    {
        return new QueuedTelegramMessage
        {
            BotId = botId,
            ChatId = _chatId,
            ThreadId = _threadId,
            ReplyToMessageId = _replyToMessageId,
            TemplateKey = _templateName,
            RawText = _rawText,
            ParseMode = _parseMode,
            TextEntities = _textEntities?.ToArray(),
            Parameters = new Dictionary<string, string>(_parameters),
            Language = _language,
            EditMessageId = _editMessageId,
            EditKeyboardOnly = _editKeyboardOnly,
            Priority = _priority,
            DisableNotification = _disableNotification,
            ProtectContent = _protectContent,
            DisableLinkPreview = _disableLinkPreview,
            ShowCaptionAboveMedia = _showCaptionAboveMedia,
            AllowPaidBroadcast = _allowPaidBroadcast,
            MessageEffectId = _messageEffectId,
            BusinessConnectionId = _businessConnectionId,
            ScheduledAt = _scheduledAt,
            MediaType = _mediaType,
            MediaUrl = _mediaUrl,
            MediaAttachment = _mediaAttachment,
            MediaCaption = _mediaCaption,
            CaptionEntities = _captionEntities?.ToArray(),
            DynamicButtonData = _dynamicData.Count > 0
                ? new Dictionary<string, List<KeyboardItemData>>(
                    _dynamicData.Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.ToList())))
                : null,
            DynamicButtonPages = _dynamicPages.Count > 0 ? new Dictionary<string, int>(_dynamicPages) : null,
            ReplyKeyboard = _replyKeyboard,
            RemoveReplyKeyboard = _removeReplyKeyboard,
            ForceReply = _forceReply,
            InlineKeyboard = _inlineKeyboard
        };
    }

    public async Task<SendResult> SendAsync(CancellationToken ct = default)
    {
        var message = Build();
        var botConfig = botProvider.GetConfiguration(botId);
        var context = new SendContext
        {
            Message = message,
            Bot = botConfig,
            CancellationToken = ct
        };

        if (_bypassRateLimit)
            context.Items["BypassRateLimit"] = true;

        return await sendPipeline.SendAsync(context, ct).ConfigureAwait(false);
    }

    public async Task<SendResult> QueueAsync(CancellationToken ct = default)
    {
        if (queueBackend == null)
            return await SendAsync(ct).ConfigureAwait(false);

        var message = Build();
        await queueBackend.EnqueueAsync(message, ct).ConfigureAwait(false);
        return new SendResult { Success = true };
    }

    public Task<SendResult> SendOrQueueAsync(CancellationToken ct = default)
    {
        return queueBackend != null ? QueueAsync(ct) : SendAsync(ct);
    }
}
