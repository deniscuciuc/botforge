# CommandBot

Demonstrates command routing with multiple handlers, each decorated with `[TelegramCommand]` attributes.

## Features

- **Multiple command handlers** — `/start`, `/help`, `/settings`, `/about`
- **Command arguments** — `/settings` accepts arguments to demonstrate parameter parsing
- **Attribute-based routing** — each command maps to a handler class automatically
- **HTML formatting** — responses use Telegram HTML parse mode

## How It Works

Each command handler is a class implementing `ICommandHandler` and decorated with `[TelegramCommand("/command")]`:

```csharp
[TelegramCommand("/help")]
public class HelpHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId)
            .WithHtml("ℹ️ <b>Help</b>...")
            .SendAsync(ct);
    }
}
```

The framework:

1. **Discovers** all handler classes at startup via reflection
2. **Registers** command routes from `[TelegramCommand]` attributes
3. **Routes** incoming updates to matching handlers
4. **Injects** dependencies (like `ITelegramMessageService`) via constructor injection

## How to Run

1. Create a bot via [@BotFather](https://t.me/BotFather) and copy the token
2. Update `appsettings.json`:

    ```json
   { "Telegram": { "BotToken": "123456:ABC-DEF..." } }
   ```

3. Run:

    ```bash
    dotnet run
    ```

4. Send `/start`, `/help`, `/settings key value`, or `/about` to your bot

## Project Structure

```text
CommandBot/
├── Program.cs                  # Host setup
├── Handlers/
│   └── CommandHandlers.cs      # StartHandler, HelpHandler, SettingsHandler, AboutHandler
├── appsettings.json
└── CommandBot.csproj
```
