# ConversationBot

Demonstrates multi-step conversation flows with state management, input validation, and reply keyboards.

## Features

- **Registration flow** — 3-step form (name → email → age) with input validation
- **Survey flow** — quick rating using reply keyboard buttons
- **Donation flow** — preset amounts via reply keyboard + custom amount with force reply
- **State management** — `IConversationStateStore` tracks each user's current step
- **Input validation** — email format check, numeric age range, positive amounts
- **Reply keyboards** — `WithReplyKeyboard()` for quick selection, `WithForceReply()` for text input
- **Keyboard cleanup** — `RemoveReplyKeyboard()` after flows complete

## How It Works

Conversation flows use `IConversationStateStore` to maintain per-user state:

```csharp
// Start a flow
await conversations.SetStateAsync(context.UserId, "register:name");

// In text handler, check current state
var state = await conversations.GetStateAsync(context.UserId);
switch (state)
{
    case "register:name":
        await conversations.SetDataAsync(context.UserId, "name", context.Text);
        await conversations.SetStateAsync(context.UserId, "register:email");
        // Ask next question...
        break;
}

// Finish flow
await conversations.ClearAsync(context.UserId);
```

The `[TextMessage]` handler acts as a router for conversation steps based on the user's current state.

## How to Run

1. Create a bot via [@BotFather](https://t.me/BotFather)
2. Update `appsettings.json` with your bot token
3. Run:
   ```bash
   dotnet run
   ```
4. Send `/register`, `/survey`, or `/donate` to start different conversation flows

## Project Structure

```
ConversationBot/
├── Program.cs
├── Handlers/
│   ├── CommandHandlers.cs          # /start, /register, /survey, /donate
│   └── ConversationStepHandler.cs  # Text handler routing conversation steps
├── appsettings.json
└── ConversationBot.csproj
```
