# MultiLanguageBot

Demonstrates multi-language support with template localization and user-specific language preferences.

## Features

- **Three languages** — English, Russian, Ukrainian
- **Template-based localization** — all messages loaded from `MessageTemplate` translations
- **User locale persistence** — remembers each user's language choice (in-memory)
- **Auto-detection** — defaults to the Telegram client's language code
- **Styled reply keyboards** — language selection uses Telegram button styles and optional custom emoji icons
- **Request buttons** — `/share` demonstrates reply-keyboard contact and location requests end to end
- **Direct entity builder APIs** — `/share` responses use explicit bold/code entities without HTML markup
- **Custom `IUserLocaleResolver`** — stores per-user language preferences

## How It Works

Templates are registered with translations for each supported language:

```csharp
templateStore.AddOrReplace(new MessageTemplate
{
    Name = "welcome",
    Text = new MessageTemplateText
    {
        Translations = new Dictionary<string, string>
        {
            ["en"] = "👋 <b>Welcome!</b>...",
            ["ru"] = "👋 <b>Добро пожаловать!</b>...",
            ["uk"] = "👋 <b>Ласкаво просимо!</b>..."
        }
    }
});
```

The `UserLocaleStore` implements `IUserLocaleResolver` and maintains per-user language preferences. When a user selects
a language via the `/lang` command's reply keyboard, the locale is stored and used for all subsequent template
rendering.

The `/share` command uses the richer reply-keyboard API to request a contact or location directly from the Telegram
client, and the follow-up handlers respond with explicit message entities instead of parse-mode markup.

## How to Run

1. Create a bot via [@BotFather](https://t.me/BotFather)
2. Update `appsettings.json` with your bot token
3. Run:

    ```bash
   dotnet run
   ```

4. Send `/start` to see the welcome message in your default language
5. Send `/lang` to choose a different language
6. Send `/share` in a private chat to try contact/location request buttons

## Project Structure

```text
MultiLanguageBot/
├── Program.cs                      # Setup + template registration + UserLocaleStore
├── Handlers/
│   └── LanguageHandlers.cs         # /start, /lang, /share, and contact/location handlers
├── appsettings.json
└── MultiLanguageBot.csproj
```
