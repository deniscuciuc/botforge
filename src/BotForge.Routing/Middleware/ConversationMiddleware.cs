using BotForge.Core;
using BotForge.Routing.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BotForge.Routing.Middleware;

/// <summary>
/// Middleware that intercepts updates when a conversation is active.
/// If a user has an active conversation step, routes to the conversation handler instead of normal routing.
/// </summary>
public class ConversationMiddleware(
    IConversationStateStore stateStore,
    ILogger<ConversationMiddleware> logger)
    : ITelegramMiddleware
{
    public async Task InvokeAsync(TelegramUpdateContext context, TelegramUpdateDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.ChatId is not { } chatId || context.UserId is not { } userId)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var step = await stateStore.GetAsync(chatId, userId, context.CancellationToken).ConfigureAwait(false);
        if (step == null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        // Extract input text from the update
        var input = context.RawUpdate.Message?.Text
                    ?? context.RawUpdate.CallbackQuery?.Data;

        if (input == null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        logger.LogDebug("Routing to conversation handler {Handler}, step {Step} for user {UserId}",
            step.HandlerType.Name, step.StepId, userId);

        var handler = (IConversationHandler)ActivatorUtilities.CreateInstance(
            context.RequestServices, step.HandlerType);

        var stepContext = new ConversationStepContext(context, step, input);
        var result = await handler.HandleStepAsync(stepContext, context.CancellationToken).ConfigureAwait(false);

        if (result.Completed)
        {
            await stateStore.ClearAsync(chatId, userId, context.CancellationToken).ConfigureAwait(false);
            logger.LogDebug("Conversation completed for user {UserId}", userId);
        }
        else if (result.NextStep != null)
        {
            await stateStore.SetAsync(chatId, userId, result.NextStep, context.CancellationToken).ConfigureAwait(false);
            logger.LogDebug("Conversation moved to step {Step} for user {UserId}",
                result.NextStep.StepId, userId);
        }
    }
}
