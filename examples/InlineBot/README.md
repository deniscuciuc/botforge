# InlineBot

Demonstrates inline query mode — search and share content directly from any chat.

## Features

- **Inline queries** — type `@YourBotName query` in any chat to search
- **Article results** — returns searchable articles with titles and descriptions
- **Category search** — search for "colors" or "animals" to see matching results
- **Free text search** — searches across titles and descriptions
- **Chosen result tracking** — logs which results users selected

## How It Works

The bot uses `[InlineQuery]` and `[ChosenInlineResult]` attributes:

```csharp
[InlineQuery]
public class SearchHandler(ITelegramBotClientProvider botProvider) : IInlineQueryHandler
{
    public async Task HandleAsync(InlineQueryContext context, CancellationToken ct)
    {
        var query = context.Query.Trim();
        var client = botProvider.GetClient(context.BotId);

        var results = new List<InlineQueryResult>();
        // ... build results from database ...

        await client.AnswerInlineQuery(context.InlineQuery.Id, results, 10, cancellationToken: ct);
    }
}
```

**Important:** Enable inline mode in [@BotFather](https://t.me/BotFather):

1. Send `/mybots` → select your bot
2. Bot Settings → Inline Mode → Turn on

## How to Run

1. Create a bot via [@BotFather](https://t.me/BotFather) and **enable inline mode**
2. Update `appsettings.json` with your bot token
3. Run:

    ```bash
   dotnet run
   ```

4. In any Telegram chat, type `@YourBotName colors` or `@YourBotName animals`

## Project Structure

```text
InlineBot/
├── Program.cs
├── Handlers/
│   └── InlineHandlers.cs   # SearchHandler (inline query) + ChosenResultHandler
├── appsettings.json
└── InlineBot.csproj
```
