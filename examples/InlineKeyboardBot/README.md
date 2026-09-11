# InlineKeyboardBot

Demonstrates inline keyboard navigation with callback queries and menu-based UI.

## Features

- **Main menu** with inline keyboard buttons (Categories, About, Settings)
- **Template-backed main menu** loaded from YAML at startup
- **Styled buttons** using Telegram button styles (`Primary`, `Success`, `Danger`)
- **Premium/custom emoji support** in message text and button icons
- **Callback query routing** — each button press routes to a separate handler
- **Message editing** — navigation updates the existing message instead of sending new ones
- **Back navigation** — return to previous menu screens

## How It Works

The bot uses `[CallbackQuery("pattern")]` attributes to route button presses:

```csharp
[CallbackQuery("menu:categories")]
public class CategoriesHandler(ITelegramMessageService messages) : ICallbackQueryHandler
{
    public async Task HandleAsync(CallbackQueryContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId)
            .EditMessage(context.MessageId)  // Edit instead of new message
            .WithHtml("📋 <b>Categories</b>...")
            .SendAsync(ct);
    }
}
```

Key patterns demonstrated:

1. **Inline keyboard creation** using `InlineKeyboardMarkup`
2. **Telegram button styles** via `KeyboardButtonStyle`
3. **Template-based menus** via `.WithTemplate("main_menu")`
4. **Custom emoji text entities** via `<tg-emoji emoji-id="...">fallback</tg-emoji>` in YAML
5. **Callback data routing** with `[CallbackQuery]` attributes
6. **Message editing** via `.EditMessage(messageId)` for smooth navigation

## Template Sample

The main menu template lives in [Templates/main_menu.yml](Templates/main_menu.yml) and demonstrates:

- HTML template text with a Telegram custom emoji tag
- Styled inline buttons (`Primary`, `Success`, `Danger`)
- Premium/custom emoji button icons via `IconCustomEmojiId`
- Callback-based routing compatible with the existing handlers

## How to Run

1. Create a bot via [@BotFather](https://t.me/BotFather)
2. Update `appsettings.json` with your bot token
3. Run:

    ```bash
   dotnet run
   ```

4. Send `/start` and navigate the menu with inline buttons

## Project Structure

```text
InlineKeyboardBot/
├── Program.cs
├── Handlers/
│   └── KeyboardHandlers.cs   # Menu, Categories, About, Settings, Back handlers
├── Templates/
│   └── main_menu.yml         # Template-based main menu with styled buttons and premium icons
├── appsettings.json
└── InlineKeyboardBot.csproj
```
