# EchoBot

A minimal Telegram bot that echoes back every text message it receives.

## Features

- **Command handling** — responds to `/start` with a welcome message
- **Text message routing** — catches all text messages and echoes them back
- **Long polling** — receives updates via Telegram long polling (no server required)

## How It Works

The bot uses three framework components:

1. **Messaging** (`AddTelegramMessaging`) — configures the bot token and message sending pipeline
2. **Routing** (`AddTelegramRouting`) — discovers handler classes and routes incoming updates
3. **Consumer** (`AddTelegramConsumer`) — connects to Telegram via long polling and feeds updates into the pipeline

Handlers are discovered automatically via reflection:

- `StartHandler` — decorated with `[TelegramCommand("/start")]`, handles the `/start` command
- `EchoHandler` — decorated with `[TextMessage]`, catches all text messages and echoes them

## How to Run

1. Create a bot via [@BotFather](https://t.me/BotFather) and copy the token
2. Update `appsettings.json` with your bot token:
   ```json
   {
     "Telegram": {
       "BotToken": "123456:ABC-DEF..."
     }
   }
   ```
3. Run the project:
   ```bash
   dotnet run
   ```
4. Open Telegram and send `/start` to your bot

## Project Structure

```
EchoBot/
├── Program.cs              # Host setup, DI configuration
├── Handlers/
│   └── EchoHandlers.cs     # StartHandler + EchoHandler
├── appsettings.json        # Bot token configuration
└── EchoBot.csproj          # Project file
```
