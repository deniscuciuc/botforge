# RequestSelectionBot

Demonstrates Telegram reply-keyboard request buttons for user and chat selection, plus the matching `UsersSharedMessage`
and `ChatSharedMessage` handlers.

## Features

- **RequestUsers button** — asks Telegram to share selected premium users with the bot
- **RequestChat button** — asks Telegram to share a group or supergroup chat with the bot
- **Typed service-message handlers** — handles `UsersSharedContext` and `ChatSharedContext`
- **Styled reply keyboard** — uses button styles and premium custom emoji icons
- **Repeatable flow** — the selection keyboard stays available after each response

## How It Works

The `/start` command sends a reply keyboard with two advanced Telegram buttons:

- `RequestUsers` using `KeyboardButtonRequestUsers`
- `RequestChat` using `KeyboardButtonRequestChat`

When the user completes either selection, Telegram sends a service message back to the bot. The framework routes those
updates into dedicated handlers decorated with `[UsersSharedMessage]` and `[ChatSharedMessage]`.

## How to Run

1. Create a bot via [@BotFather](https://t.me/BotFather)
2. Update `appsettings.json` with your bot token
3. Run:

   ```bash
   dotnet run
   ```

4. Open a private chat with the bot and send `/start`
5. Tap `Pick premium users` or `Pick a group chat`
6. Inspect the bot response for the routed `UsersShared` or `ChatShared` payload

## Notes

- Telegram request buttons only work in private chats.
- `RequestChat` returns metadata only if it was requested by the button options.
- A shared chat ID does not guarantee the bot already has access to that chat.

## Project Structure

```text
RequestSelectionBot/
├── Program.cs
├── Handlers/
│   └── SelectionHandlers.cs   # /start, /remove, UsersShared, ChatShared
├── appsettings.json
└── RequestSelectionBot.csproj
```
