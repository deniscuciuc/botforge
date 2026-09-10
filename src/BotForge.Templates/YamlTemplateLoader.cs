using BotForge.Core;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BotForge.Templates;

public class YamlTemplateLoader(
    IMessageTemplateStore store,
    IEmojiRegistry emojis,
    ILogger<YamlTemplateLoader> logger)
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(PascalCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public void LoadFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            logger.LogWarning("Template directory '{Directory}' does not exist", directory);
            return;
        }

        var ymlFiles = Directory.GetFiles(directory, "*.yml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(directory, "*.yaml", SearchOption.AllDirectories))
            .ToList();

        var loadedCount = 0;

        foreach (var file in ymlFiles)
            try
            {
                var content = File.ReadAllText(file);
                var fileName = Path.GetFileNameWithoutExtension(file);

                // Check if it's an emoji file
                if (fileName.Equals("emojis", StringComparison.OrdinalIgnoreCase))
                {
                    LoadEmojis(content);
                    continue;
                }

                // Try to load as a single template or a list
                if (content.TrimStart().StartsWith('-'))
                {
                    var templates = _deserializer.Deserialize<List<YamlMessageTemplate>>(content);
                    if (templates != null)
                        foreach (var t in templates)
                        {
                            store.AddOrReplace(MapTemplate(t));
                            loadedCount++;
                        }
                }
                else
                {
                    var template = _deserializer.Deserialize<YamlMessageTemplate>(content);
                    if (template?.Name != null)
                    {
                        store.AddOrReplace(MapTemplate(template));
                        loadedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load template from '{File}'", file);
            }

        logger.LogInformation("Loaded {Count} templates from '{Directory}'", loadedCount, directory);
    }

    public void LoadEmojiFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            logger.LogWarning("Emoji file '{File}' does not exist", filePath);
            return;
        }

        var content = File.ReadAllText(filePath);
        LoadEmojis(content);
    }

    private void LoadEmojis(string yamlContent)
    {
        var emojiConfig = _deserializer.Deserialize<YamlEmojiConfig>(yamlContent);
        if (emojiConfig?.Emojis != null)
        {
            emojis.Load(emojiConfig.Emojis);
            logger.LogInformation("Loaded {Count} emojis", emojiConfig.Emojis.Count);
        }
    }

    private static MessageTemplate MapTemplate(YamlMessageTemplate yaml)
    {
        return new MessageTemplate
        {
            Name = yaml.Name ?? "unnamed",
            Category = yaml.Category,
            ParseMode =
                Enum.TryParse<TelegramParseMode>(yaml.ParseMode, true, out var pm) ? pm : TelegramParseMode.Html,
            IsPartial = yaml.IsPartial,
            IsLayout = yaml.IsLayout,
            Extends = yaml.Extends,
            Text = yaml.Text != null
                ? new MessageTemplateText
                {
                    Key = yaml.Text.Key,
                    LocalizationKey = yaml.Text.LocalizationKey,
                    Translations = yaml.Text.Translations ?? new Dictionary<string, string>()
                }
                : new MessageTemplateText(),
            Buttons = yaml.Buttons?.Select(MapButton).ToList() ?? [],
            DynamicButtons = yaml.DynamicButtons?.Select(MapDynamicButton).ToList() ?? [],
            Blocks = yaml.Blocks?.ToDictionary(
                kvp => kvp.Key,
                kvp => new MessageTemplateText { Translations = kvp.Value ?? new Dictionary<string, string>() })
        };
    }

    private static MessageTemplateButton MapButton(YamlButton yaml)
    {
        return new MessageTemplateButton
        {
            Text = new MessageTemplateText
            {
                Key = yaml.Text?.Key,
                LocalizationKey = yaml.Text?.LocalizationKey,
                Translations = yaml.Text?.Translations ?? new Dictionary<string, string>()
            },
            Type = Enum.TryParse<TelegramButtonType>(yaml.Type, true, out var bt) ? bt : TelegramButtonType.Callback,
            Value = yaml.Value ?? string.Empty,
            Style = TryParseButtonStyle(yaml.Style),
            IconCustomEmojiId = yaml.IconCustomEmojiId,
            LoginForwardText = yaml.LoginForwardText,
            LoginBotUsername = yaml.LoginBotUsername,
            LoginRequestWriteAccess = yaml.LoginRequestWriteAccess,
            Row = yaml.Row,
            Order = yaml.Order,
            ShowIf = yaml.ShowIf
        };
    }

    private static MessageTemplateDynamicButton MapDynamicButton(YamlDynamicButton yaml)
    {
        return new MessageTemplateDynamicButton
        {
            Name = yaml.Name ?? string.Empty,
            Text = new MessageTemplateText
            {
                Key = yaml.Text?.Key,
                LocalizationKey = yaml.Text?.LocalizationKey,
                Translations = yaml.Text?.Translations ?? new Dictionary<string, string>()
            },
            Type = Enum.TryParse<TelegramButtonType>(yaml.Type, true, out var bt) ? bt : TelegramButtonType.Callback,
            Value = yaml.Value ?? string.Empty,
            Style = TryParseButtonStyle(yaml.Style),
            IconCustomEmojiId = yaml.IconCustomEmojiId,
            LoginForwardText = yaml.LoginForwardText,
            LoginBotUsername = yaml.LoginBotUsername,
            LoginRequestWriteAccess = yaml.LoginRequestWriteAccess,
            RowStart = yaml.RowStart,
            ItemsPerRow = yaml.ItemsPerRow,
            ItemsPerPage = yaml.ItemsPerPage,
            PaginationCallbackPrefix = yaml.PaginationCallbackPrefix,
            PaginationRow = yaml.PaginationRow,
            PaginationButtons = yaml.PaginationButtons != null
                ? new MessageTemplatePaginationButtons
                {
                    Previous = yaml.PaginationButtons.Previous ?? new Dictionary<string, string>(),
                    Next = yaml.PaginationButtons.Next ?? new Dictionary<string, string>(),
                    Counter = yaml.PaginationButtons.Counter
                }
                : null
        };
    }

    private static TelegramButtonStyle? TryParseButtonStyle(string? style)
    {
        return Enum.TryParse<TelegramButtonStyle>(style, true, out var parsed)
            ? parsed
            : null;
    }

    // YAML deserialization models (internal)
    private sealed class YamlEmojiConfig
    {
        public Dictionary<string, string>? Emojis { get; set; }
    }

    private sealed class YamlMessageTemplate
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? ParseMode { get; set; }
        public bool IsPartial { get; set; }
        public bool IsLayout { get; set; }
        public string? Extends { get; set; }
        public YamlText? Text { get; set; }
        public List<YamlButton>? Buttons { get; set; }
        public List<YamlDynamicButton>? DynamicButtons { get; set; }
        public Dictionary<string, Dictionary<string, string>>? Blocks { get; set; }
    }

    private sealed class YamlText
    {
        public string? Key { get; set; }
        public string? LocalizationKey { get; set; }
        public Dictionary<string, string>? Translations { get; set; }
    }

    private sealed class YamlButton
    {
        public YamlText? Text { get; set; }
        public string? Type { get; set; }
        public string? Value { get; set; }
        public string? Style { get; set; }
        public string? IconCustomEmojiId { get; set; }
        public string? LoginForwardText { get; set; }
        public string? LoginBotUsername { get; set; }
        public bool LoginRequestWriteAccess { get; set; }
        public int Row { get; set; }
        public int Order { get; set; }
        public string? ShowIf { get; set; }
    }

    private sealed class YamlDynamicButton
    {
        public string? Name { get; set; }
        public YamlText? Text { get; set; }
        public string? Type { get; set; }
        public string? Value { get; set; }
        public string? Style { get; set; }
        public string? IconCustomEmojiId { get; set; }
        public string? LoginForwardText { get; set; }
        public string? LoginBotUsername { get; set; }
        public bool LoginRequestWriteAccess { get; set; }
        public int RowStart { get; set; }
        public int ItemsPerRow { get; set; } = 1;
        public int ItemsPerPage { get; set; } = 5;
        public string? PaginationCallbackPrefix { get; set; }
        public int? PaginationRow { get; set; }
        public YamlPaginationButtons? PaginationButtons { get; set; }
    }

    private sealed class YamlPaginationButtons
    {
        public Dictionary<string, string>? Previous { get; set; }
        public Dictionary<string, string>? Next { get; set; }
        public string? Counter { get; set; }
    }
}
