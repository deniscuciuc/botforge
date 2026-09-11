using Microsoft.Extensions.Logging.Abstractions;
using TeleForge.Core;

namespace TeleForge.Templates.Tests;

public class YamlTemplateLoaderTests
{
    [Fact]
    public void LoadFromDirectory_MapsStyledLoginButtonMetadata()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"teleforge-templates-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            File.WriteAllText(Path.Combine(tempDirectory, "styled.yml"),
                """
                Name: styled_login
                ParseMode: Html
                Text:
                  Translations:
                    en: "Hello"
                Buttons:
                  - Text:
                      Translations:
                        en: "Login"
                    Type: Login
                    Value: "https://example.com/login"
                    Style: Danger
                    IconCustomEmojiId: "premium-icon"
                    LoginForwardText: "Authorize"
                    LoginBotUsername: "example_bot"
                    LoginRequestWriteAccess: true
                    Row: 0
                    Order: 0
                """);

            var store = new InMemoryMessageTemplateStore();
            var loader = new YamlTemplateLoader(store, new EmojiRegistry(), NullLogger<YamlTemplateLoader>.Instance);

            loader.LoadFromDirectory(tempDirectory);

            var template = store.Get("styled_login");
            Assert.NotNull(template);

            var button = Assert.Single(template!.Buttons);
            Assert.Equal(TelegramButtonType.Login, button.Type);
            Assert.Equal("https://example.com/login", button.Value);
            Assert.Equal(TelegramButtonStyle.Danger, button.Style);
            Assert.Equal("premium-icon", button.IconCustomEmojiId);
            Assert.Equal("Authorize", button.LoginForwardText);
            Assert.Equal("example_bot", button.LoginBotUsername);
            Assert.True(button.LoginRequestWriteAccess);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void LoadFromDirectory_MapsDynamicButtonLocalizationKey()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"teleforge-templates-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            File.WriteAllText(Path.Combine(tempDirectory, "dynamic.yml"),
                """
                Name: localized_dynamic
                ParseMode: Html
                Text:
                  Translations:
                    en: "Catalog"
                DynamicButtons:
                  - Name: items
                    Text:
                      LocalizationKey: "buttons.catalog.item"
                      Translations:
                        en: "Fallback"
                    Type: Callback
                    Value: "item:{ItemId}"
                """);

            var store = new InMemoryMessageTemplateStore();
            var loader = new YamlTemplateLoader(store, new EmojiRegistry(), NullLogger<YamlTemplateLoader>.Instance);

            loader.LoadFromDirectory(tempDirectory);

            var template = store.Get("localized_dynamic");
            Assert.NotNull(template);

            var button = Assert.Single(template!.DynamicButtons);
            Assert.Equal("buttons.catalog.item", button.Text.LocalizationKey);
            Assert.Equal("Fallback", button.Text.Translations["en"]);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);
        }
    }
}
