# Conversations

## Overview

Conversations provide a state machine for multi-step user interactions. When a conversation is active, user input is routed to the conversation handler instead of normal command/callback routing.

## How It Works

```
1. Command handler starts conversation → SetAsync(chatId, userId, step)
2. User sends next message → ConversationMiddleware intercepts
3. Middleware creates handler → calls HandleStepAsync()
4. Handler returns:
   - GoTo(nextStep) → update state, wait for next input
   - Complete()     → clear state, resume normal routing
   - Invalid(msg)   → state unchanged, re-prompt user
```

## Starting a Conversation

```csharp
[TelegramCommand("/register")]
public class RegisterCommand(
    IConversationStateStore stateStore,
    ITelegramMessageService messaging) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await stateStore.SetAsync(
            context.ChatId!.Value,
            context.UserId!.Value,
            new ConversationStep
            {
                StepId = "ask_name",
                HandlerType = typeof(RegistrationHandler),
                Data = new Dictionary<string, string>()
            },
            ct);

        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("What is your name?")
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}
```

## Handling Steps

```csharp
public class RegistrationHandler(
    IConversationStateStore stateStore,
    ITelegramMessageService messaging) : IConversationHandler
{
    public async Task<ConversationResult> HandleStepAsync(
        ConversationStepContext context, CancellationToken ct)
    {
        switch (context.Step.StepId)
        {
            case "ask_name":
                if (string.IsNullOrWhiteSpace(context.Input))
                    return ConversationResult.Invalid("Please enter a valid name.");

                await messaging.CreateMessage(context.BotId)
                    .ToChat(context.ChatId!.Value)
                    .WithText("How old are you?")
                    .SendAsync(ct);

                return ConversationResult.GoTo(new ConversationStep
                {
                    StepId = "ask_age",
                    HandlerType = typeof(RegistrationHandler),
                    Data = new Dictionary<string, string>
                    {
                        ["Name"] = context.Input
                    }
                });

            case "ask_age":
                if (!int.TryParse(context.Input, out var age) || age < 1 || age > 150)
                    return ConversationResult.Invalid("Please enter a valid age.");

                var name = context.GetStepData("Name");

                await messaging.CreateMessage(context.BotId)
                    .ToChat(context.ChatId!.Value)
                    .WithText($"Registration complete!\nName: {name}\nAge: {age}")
                    .SendAsync(ct);

                return ConversationResult.Complete();

            default:
                return ConversationResult.Complete();
        }
    }
}
```

## IConversationHandler

```csharp
public interface IConversationHandler
{
    Task<ConversationResult> HandleStepAsync(ConversationStepContext context, CancellationToken ct);
}
```

## ConversationStepContext

| Property | Type | Description |
|----------|------|-------------|
| `UpdateContext` | `TelegramUpdateContext` | Full update context |
| `Step` | `ConversationStep` | Current step with ID, handler type, data |
| `Input` | `string` | User's text message or callback data |
| `ChatId` | `long?` | Chat ID |
| `UserId` | `long?` | User ID |
| `BotId` | `string` | Bot key |
| `CancellationToken` | `CancellationToken` | Cancellation token |
| `RequestServices` | `IServiceProvider` | Scoped DI container |

**Helper method:** `GetStepData(string key)` — shortcut for `Step.Data[key]`, returns `null` if key not found.

## ConversationStep

```csharp
public class ConversationStep
{
    public required string StepId { get; init; }
    public required Type HandlerType { get; init; }
    public Dictionary<string, string> Data { get; init; } = [];
    public DateTimeOffset? ExpiresAt { get; init; }
}
```

| Property | Description |
|----------|-------------|
| `StepId` | Identifies the current step (e.g., `"ask_name"`, `"confirm"`) |
| `HandlerType` | The `IConversationHandler` type to instantiate |
| `Data` | Key-value bag for passing data between steps |
| `ExpiresAt` | Optional expiration — expired steps are auto-cleared |

## ConversationResult

| Factory | Effect |
|---------|--------|
| `ConversationResult.Complete()` | Ends the conversation, clears state |
| `ConversationResult.GoTo(step)` | Moves to the next step, updates state |
| `ConversationResult.Invalid(message)` | Stays on current step (handler should re-prompt) |

## IConversationStateStore

```csharp
public interface IConversationStateStore
{
    Task<ConversationStep?> GetAsync(long chatId, long userId, CancellationToken ct);
    Task SetAsync(long chatId, long userId, ConversationStep step, CancellationToken ct);
    Task ClearAsync(long chatId, long userId, CancellationToken ct);
}
```

State is keyed by `(chatId, userId)` — each user in each chat has independent conversation state.

### In-Memory Store (Default)

Uses `ConcurrentDictionary<(long, long), ConversationStep>`. Registered as `TryAddSingleton` — automatically used unless overridden.

- `GetAsync` auto-clears expired steps (checks `ExpiresAt`)
- Suitable for single-process; state lost on restart

### Custom Store

Override with a persistent implementation (e.g., Redis, database):

```csharp
// Register before AddTelegramRouting to override
builder.Services.AddSingleton<IConversationStateStore, RedisConversationStateStore>();

builder.Services.AddTelegramRouting(routing =>
{
    routing.AddHandlersFromAssembly(typeof(Program).Assembly);
});
```

## Step Expiration

Set `ExpiresAt` to automatically expire idle conversations:

```csharp
return ConversationResult.GoTo(new ConversationStep
{
    StepId = "confirm",
    HandlerType = typeof(MyHandler),
    Data = data,
    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
});
```

If the user responds after expiration, the middleware clears the state and routes the message through normal handlers.

## Cancellation

Allow users to exit conversations with a command check:

```csharp
public async Task<ConversationResult> HandleStepAsync(
    ConversationStepContext context, CancellationToken ct)
{
    if (context.Input is "/cancel")
    {
        await messaging.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("Cancelled.")
            .SendAsync(ct);

        return ConversationResult.Complete();
    }

    // Normal step logic...
}
```

## ConversationMiddleware

The middleware is part of the routing pipeline and checks for active conversations on every update:

1. Extract `chatId` and `userId` from the update
2. Query `IConversationStateStore.GetAsync(chatId, userId)`
3. If no active step → pass through to normal routing
4. Extract input text from `Message.Text` or `CallbackQuery.Data`
5. Create handler instance via `ActivatorUtilities.CreateInstance`
6. Call `HandleStepAsync` and apply the result

The middleware is registered automatically by `AddTelegramRouting`.
