using BotForge.Core;

namespace BotForge.Templates;

public interface IMessageTemplateStore
{
    MessageTemplate? Get(string name);
    IReadOnlyList<MessageTemplate> GetAll();
    void Load(IEnumerable<MessageTemplate> templates);
    void AddOrReplace(MessageTemplate template);
    bool Remove(string name);
}

public class MessageTemplate
{
    public string Name { get; set; } = null!;
    public string? Category { get; set; }
    public TelegramParseMode ParseMode { get; set; } = TelegramParseMode.Html;
    public bool IsPartial { get; set; }
    public bool IsLayout { get; set; }
    public string? Extends { get; set; }

    public MessageTemplateText Text { get; set; } = new();
    public List<MessageTemplateButton> Buttons { get; set; } = [];
    public List<MessageTemplateDynamicButton> DynamicButtons { get; set; } = [];
    public Dictionary<string, MessageTemplateText>? Blocks { get; set; }
}

public class MessageTemplateText
{
    public string? Key { get; set; }
    public string? LocalizationKey { get; set; }
    public Dictionary<string, string> Translations { get; set; } = new();
}

public class MessageTemplateButton
{
    public MessageTemplateText Text { get; set; } = new();
    public TelegramButtonType Type { get; set; } = TelegramButtonType.Callback;
    public string Value { get; set; } = null!;
    public TelegramButtonStyle? Style { get; set; }
    public string? IconCustomEmojiId { get; set; }
    public string? LoginForwardText { get; set; }
    public string? LoginBotUsername { get; set; }
    public bool LoginRequestWriteAccess { get; set; }
    public int Row { get; set; }
    public int Order { get; set; }
    public string? ShowIf { get; set; }
}

public class MessageTemplateDynamicButton
{
    public string Name { get; set; } = null!;
    public MessageTemplateText Text { get; set; } = new();
    public TelegramButtonType Type { get; set; } = TelegramButtonType.Callback;
    public string Value { get; set; } = null!;
    public TelegramButtonStyle? Style { get; set; }
    public string? IconCustomEmojiId { get; set; }
    public string? LoginForwardText { get; set; }
    public string? LoginBotUsername { get; set; }
    public bool LoginRequestWriteAccess { get; set; }
    public int RowStart { get; set; }
    public int ItemsPerRow { get; set; } = 1;
    public int ItemsPerPage { get; set; } = 5;
    public string? PaginationCallbackPrefix { get; set; }
    public int? PaginationRow { get; set; }
    public MessageTemplatePaginationButtons? PaginationButtons { get; set; }
}

public class MessageTemplatePaginationButtons
{
    public Dictionary<string, string> Previous { get; set; } = new();
    public Dictionary<string, string> Next { get; set; } = new();
    public string? Counter { get; set; }
}
