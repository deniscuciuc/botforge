# DiceMiniGames

Mini games using Telegram's built-in dice, darts, basketball, bowling, football, and slot machine animations.

## Features

- **Multiple game types** — 🎲 Dice, 🎯 Darts, 🏀 Basketball, 🎳 Bowling, ⚽ Football, 🎰 Slots
- **Automatic scoring** — points awarded based on dice value and win threshold
- **SQLite persistence** — scores and game history stored via EF Core
- **Leaderboard** — view top 10 players across all games
- **Dice handler routing** — uses `[DiceMessage]` attribute + `IDiceHandler` interface

## How It Works

The framework's dice routing intercepts dice/animation messages:

```csharp
[DiceMessage]
public class DiceResultHandler(
    ITelegramMessageService messages,
    GameDbContext db) : IDiceHandler
{
    public async Task HandleAsync(DiceContext context, CancellationToken ct)
    {
        var emoji = context.Emoji; // 🎲, 🎯, 🏀, ⚽, 🎰, 🎳
        var value = context.Value; // Result value (1-6 for dice, etc.)
        // ... score and respond
    }
}
```

### Game Rules

| Emoji | Game       | Values | Win Threshold | Max Points |
| ----- | ---------- | ------ | ------------- | ---------- |
| 🎲    | Dice       | 1-6    | 4+            | 60         |
| 🎯    | Darts      | 1-6    | 4+            | 60         |
| 🏀    | Basketball | 1-5    | 4+            | 50         |
| ⚽    | Football   | 1-5    | 3+            | 50         |
| 🎳    | Bowling    | 1-6    | 4+            | 60         |
| 🎰    | Slots      | 1-64   | 22+           | 640        |

## How to Run

1. Create a bot via [@BotFather](https://t.me/BotFather)
2. Update `appsettings.json` with your bot token
3. Run:

    ```bash
   dotnet run
   ```

4. Send dice/darts/basketball stickers to the bot to play
5. Use `/leaderboard` to see top scores

## Project Structure

```text
DiceMiniGames/
├── Program.cs                    # Host setup, EF Core + SQLite, templates
├── Data/
│   └── GameDbContext.cs          # EF Core context, PlayerScore, GameResult models
├── Handlers/
│   ├── CommandHandlers.cs        # /start, /dice, /darts, etc., /leaderboard
│   └── DiceResultHandler.cs     # [DiceMessage] handler with scoring logic
├── appsettings.json
└── DiceMiniGames.csproj
```
