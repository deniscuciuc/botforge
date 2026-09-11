using Microsoft.Extensions.Logging;
using TeleForge.Core;

namespace TeleForge.Routing.Middleware;

public class ExceptionHandlerMiddleware(ILogger<ExceptionHandlerMiddleware> logger) : ITelegramMiddleware
{
    public async Task InvokeAsync(TelegramUpdateContext context, TelegramUpdateDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Update processing cancelled for user {UserId} in chat {ChatId}",
                context.UserId, context.ChatId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unhandled exception processing {UpdateType} from user {UserId} in chat {ChatId}",
                context.UpdateType, context.UserId, context.ChatId);

            context.Result = UpdateResult.Blocked("An internal error occurred.");
        }
    }
}
