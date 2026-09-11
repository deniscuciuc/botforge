using TeleForge.Messaging.Abstractions;
using TeleForge.Routing.Abstractions;

namespace ConversationBot.Handlers;

/// <summary>Registration flow handler — collects name, email, age.</summary>
public class RegistrationStepHandler(ITelegramMessageService messages) : IConversationHandler
{
    public async Task<ConversationResult> HandleStepAsync(ConversationStepContext context, CancellationToken ct)
    {
        switch (context.Step.StepId)
        {
            case "register:name":
                var nameData = new Dictionary<string, string>(context.Step.Data)
                {
                    ["name"] = context.Input
                };
                await messages.CreateMessage(context.BotId)
                    .ToChat(context.ChatId!.Value)
                    .WithText($"Nice to meet you, {context.Input}! Now enter your email:")
                    .WithForceReply("Enter your email")
                    .SendAsync(ct);
                return ConversationResult.GoTo(new ConversationStep
                {
                    StepId = "register:email",
                    HandlerType = typeof(RegistrationStepHandler),
                    Data = nameData
                });

            case "register:email":
                if (!context.Input.Contains('@'))
                {
                    await messages.CreateMessage(context.BotId)
                        .ToChat(context.ChatId!.Value)
                        .WithText("❌ That doesn't look like a valid email. Please try again:")
                        .WithForceReply("Enter your email")
                        .SendAsync(ct);
                    return ConversationResult.Invalid("Invalid email format");
                }

                var emailData = new Dictionary<string, string>(context.Step.Data)
                {
                    ["email"] = context.Input
                };
                await messages.CreateMessage(context.BotId)
                    .ToChat(context.ChatId!.Value)
                    .WithText("How old are you?")
                    .WithForceReply("Enter your age")
                    .SendAsync(ct);
                return ConversationResult.GoTo(new ConversationStep
                {
                    StepId = "register:age",
                    HandlerType = typeof(RegistrationStepHandler),
                    Data = emailData
                });

            case "register:age":
                if (!int.TryParse(context.Input, out var age) || age < 1 || age > 150)
                {
                    await messages.CreateMessage(context.BotId)
                        .ToChat(context.ChatId!.Value)
                        .WithText("❌ Please enter a valid age (1-150):")
                        .SendAsync(ct);
                    return ConversationResult.Invalid("Invalid age");
                }

                var name = context.GetStepData("name") ?? "?";
                var email = context.GetStepData("email") ?? "?";
                await messages.CreateMessage(context.BotId)
                    .ToChat(context.ChatId!.Value)
                    .WithHtml(
                        "✅ <b>Registration complete!</b>\n\n" +
                        $"👤 Name: {name}\n" +
                        $"📧 Email: {email}\n" +
                        $"🎂 Age: {age}")
                    .RemoveReplyKeyboard()
                    .SendAsync(ct);
                return ConversationResult.Complete();

            default:
                return ConversationResult.Complete();
        }
    }
}

/// <summary>Survey flow handler — collects rating.</summary>
public class SurveyStepHandler(ITelegramMessageService messages) : IConversationHandler
{
    public async Task<ConversationResult> HandleStepAsync(ConversationStepContext context, CancellationToken ct)
    {
        var stars = context.Input.Count(c => c == '⭐');
        if (stars == 0) stars = context.Input.Length;

        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText($"Thanks for your {stars}-star rating! 🎉")
            .RemoveReplyKeyboard()
            .SendAsync(ct);
        return ConversationResult.Complete();
    }
}

/// <summary>Donation flow handler — collects amount.</summary>
public class DonateStepHandler(ITelegramMessageService messages) : IConversationHandler
{
    public async Task<ConversationResult> HandleStepAsync(ConversationStepContext context, CancellationToken ct)
    {
        switch (context.Step.StepId)
        {
            case "donate:amount":
                if (context.Input == "Custom amount")
                {
                    await messages.CreateMessage(context.BotId)
                        .ToChat(context.ChatId!.Value)
                        .WithText("Enter a custom amount (e.g., 42):")
                        .WithForceReply("Amount in USD")
                        .SendAsync(ct);
                    return ConversationResult.GoTo(new ConversationStep
                    {
                        StepId = "donate:custom",
                        HandlerType = typeof(DonateStepHandler)
                    });
                }

                await messages.CreateMessage(context.BotId)
                    .ToChat(context.ChatId!.Value)
                    .WithText($"💸 Thank you for your {context.Input} donation! (Demo — no real payment)")
                    .RemoveReplyKeyboard()
                    .SendAsync(ct);
                return ConversationResult.Complete();

            case "donate:custom":
                if (!decimal.TryParse(context.Input, out var amount) || amount <= 0)
                {
                    await messages.CreateMessage(context.BotId)
                        .ToChat(context.ChatId!.Value)
                        .WithText("❌ Please enter a valid positive number:")
                        .SendAsync(ct);
                    return ConversationResult.Invalid("Invalid amount");
                }

                await messages.CreateMessage(context.BotId)
                    .ToChat(context.ChatId!.Value)
                    .WithText($"💸 Thank you for your ${amount} donation! (Demo — no real payment)")
                    .RemoveReplyKeyboard()
                    .SendAsync(ct);
                return ConversationResult.Complete();

            default:
                return ConversationResult.Complete();
        }
    }
}
