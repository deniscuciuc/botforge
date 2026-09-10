using BotForge.Core;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotForge.Templates.Tests;

public class KeyboardBuilderTests
{
    private readonly KeyboardBuilder _builder = new(new EmojiRegistry(), new ConditionalEvaluator());

    [Fact]
    public void BuildInlineKeyboard_AppliesStyleAndPremiumIcon()
    {
        var template = new MessageTemplate
        {
            Name = "styled",
            Buttons =
            [
                new MessageTemplateButton
                {
                    Text = new MessageTemplateText
                        { Translations = new Dictionary<string, string> { ["en"] = "Styled" } },
                    Type = TelegramButtonType.Callback,
                    Value = "styled:1",
                    Style = TelegramButtonStyle.Primary,
                    IconCustomEmojiId = "premium-emoji-id",
                    Row = 0,
                    Order = 0
                }
            ]
        };

        var markup = _builder.BuildInlineKeyboard(template, new Dictionary<string, string>(), "en");

        Assert.NotNull(markup);
        var row = Assert.Single(markup.InlineKeyboard);
        var button = Assert.Single(row);
        Assert.Equal("Styled", button.Text);
        Assert.Equal("styled:1", button.CallbackData);
        Assert.Equal(KeyboardButtonStyle.Primary, button.Style);
        Assert.Equal("premium-emoji-id", button.IconCustomEmojiId);
    }

    [Fact]
    public void BuildInlineKeyboard_SupportsWebAppLoginAndPayButtons()
    {
        var template = new MessageTemplate
        {
            Name = "variants",
            Buttons =
            [
                new MessageTemplateButton
                {
                    Text = new MessageTemplateText { Translations = new Dictionary<string, string> { ["en"] = "App" } },
                    Type = TelegramButtonType.WebApp,
                    Value = "https://example.com/app",
                    Row = 0,
                    Order = 0
                },
                new MessageTemplateButton
                {
                    Text = new MessageTemplateText
                        { Translations = new Dictionary<string, string> { ["en"] = "Login" } },
                    Type = TelegramButtonType.Login,
                    Value = "https://example.com/login",
                    LoginForwardText = "Authorize",
                    LoginBotUsername = "example_bot",
                    LoginRequestWriteAccess = true,
                    Row = 0,
                    Order = 1
                },
                new MessageTemplateButton
                {
                    Text = new MessageTemplateText
                        { Translations = new Dictionary<string, string> { ["en"] = "Pay ⭐" } },
                    Type = TelegramButtonType.Pay,
                    Value = string.Empty,
                    Row = 1,
                    Order = 0
                }
            ]
        };

        var markup = _builder.BuildInlineKeyboard(template, new Dictionary<string, string>(), "en");

        Assert.NotNull(markup);
        var rows = markup.InlineKeyboard.ToArray();
        Assert.Equal(2, rows.Length);

        var firstRow = rows[0].ToArray();
        Assert.NotNull(firstRow[0].WebApp);
        Assert.Equal("https://example.com/app", firstRow[0].WebApp!.Url);
        Assert.NotNull(firstRow[1].LoginUrl);
        Assert.Equal("https://example.com/login", firstRow[1].LoginUrl!.Url);
        Assert.Equal("Authorize", firstRow[1].LoginUrl!.ForwardText);
        Assert.Equal("example_bot", firstRow[1].LoginUrl!.BotUsername);
        Assert.True(firstRow[1].LoginUrl!.RequestWriteAccess);

        var payButton = Assert.Single(rows[1]);
        Assert.True(payButton.Pay);
    }

    [Fact]
    public void BuildInlineKeyboard_ResolvesDynamicButtonMetadata()
    {
        var template = new MessageTemplate
        {
            Name = "dynamic",
            DynamicButtons =
            [
                new MessageTemplateDynamicButton
                {
                    Name = "items",
                    Text = new MessageTemplateText
                        { Translations = new Dictionary<string, string> { ["en"] = "{Label}" } },
                    Type = TelegramButtonType.Url,
                    Value = "https://example.com/{Slug}",
                    IconCustomEmojiId = "{EmojiId}",
                    Style = TelegramButtonStyle.Success,
                    RowStart = 0,
                    ItemsPerRow = 1,
                    ItemsPerPage = 5
                }
            ]
        };

        var dynamicData = new Dictionary<string, IReadOnlyList<KeyboardItemData>>
        {
            ["items"] =
            [
                KeyboardItemData.FromValues(("Label", "Catalog"), ("Slug", "catalog"), ("EmojiId", "dyn-emoji"))
            ]
        };

        var markup = _builder.BuildInlineKeyboard(template, new Dictionary<string, string>(), "en", dynamicData);

        Assert.NotNull(markup);
        var button = Assert.Single(Assert.Single(markup.InlineKeyboard));
        Assert.Equal("Catalog", button.Text);
        Assert.Equal("https://example.com/catalog", button.Url);
        Assert.Equal("dyn-emoji", button.IconCustomEmojiId);
        Assert.Equal(KeyboardButtonStyle.Success, button.Style);
    }

    [Fact]
    public void BuildInlineKeyboard_ThrowsWhenWebAppButtonHasNoValue()
    {
        var template = new MessageTemplate
        {
            Name = "invalid",
            Buttons =
            [
                new MessageTemplateButton
                {
                    Text = new MessageTemplateText
                        { Translations = new Dictionary<string, string> { ["en"] = "Broken" } },
                    Type = TelegramButtonType.WebApp,
                    Value = string.Empty,
                    Row = 0,
                    Order = 0
                }
            ]
        };

        Assert.Throws<InvalidOperationException>(() =>
            _builder.BuildInlineKeyboard(template, new Dictionary<string, string>(), "en"));
    }
}
