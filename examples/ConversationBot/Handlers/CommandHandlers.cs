using BotForge.Messaging.Abstractions;
using BotForge.Routing.Abstractions;
using BotForge.Routing.Attributes;
using BotForge.Routing.Handlers;

namespace ConversationBot.Handlers;

[TelegramCommand("/start", "Show welcome message")]
public class StartHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "👋 <b>Welcome to ConversationBot!</b>\n\n" +
                "I demonstrate multi-step conversation flows.\n\n" +
                "Try these commands:\n" +
                "/register — Start a registration form\n" +
                "/survey — Take a quick survey\n" +
                "/donate — Make a donation (demo)")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/register", "Start registration")]
public class RegisterHandler(
    ITelegramMessageService messages,
    IConversationStateStore conversations) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var chatId = context.ChatId ?? 0;
        var userId = context.UserId ?? 0;

        await conversations.SetAsync(chatId, userId, new ConversationStep
        {
            StepId = "register:name",
            HandlerType = typeof(RegistrationStepHandler)
        }, ct);

        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("📝 Let's register! What's your name?")
            .WithForceReply("Enter your name")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/survey", "Take a quick survey")]
public class SurveyHandler(
    ITelegramMessageService messages,
    IConversationStateStore conversations) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var chatId = context.ChatId ?? 0;
        var userId = context.UserId ?? 0;

        await conversations.SetAsync(chatId, userId, new ConversationStep
        {
            StepId = "survey:rating",
            HandlerType = typeof(SurveyStepHandler)
        }, ct);

        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("📊 Quick survey! How would you rate this framework?")
            .WithReplyKeyboard(
                [["⭐", "⭐⭐", "⭐⭐⭐", "⭐⭐⭐⭐", "⭐⭐⭐⭐⭐"]],
                true,
                true,
                "Choose a rating")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/donate", "Make a donation (demo)")]
public class DonateHandler(
    ITelegramMessageService messages,
    IConversationStateStore conversations) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var chatId = context.ChatId ?? 0;
        var userId = context.UserId ?? 0;

        await conversations.SetAsync(chatId, userId, new ConversationStep
        {
            StepId = "donate:amount",
            HandlerType = typeof(DonateStepHandler)
        }, ct);

        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("💰 How much would you like to donate?")
            .WithReplyKeyboard(
                [["$5", "$10", "$25"], ["$50", "$100", "Custom amount"]],
                true,
                true)
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}
