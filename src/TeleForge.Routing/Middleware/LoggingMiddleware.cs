using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TeleForge.Core;

namespace TeleForge.Routing.Middleware;

public class LoggingMiddleware(ILogger<LoggingMiddleware> logger) : ITelegramMiddleware
{
    public async Task InvokeAsync(TelegramUpdateContext context, TelegramUpdateDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var sw = Stopwatch.StartNew();

        logger.LogDebug(
            "Processing update {UpdateType} from user {UserId} in chat {ChatId} [Bot: {BotId}]",
            context.UpdateType, context.UserId, context.ChatId, context.BotId);

        try
        {
            await next(context).ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            logger.LogDebug(
                "Finished processing {UpdateType} in {ElapsedMs}ms. Result: {ResultStatus}",
                context.UpdateType, sw.ElapsedMilliseconds,
                context.Result?.Success == false ? context.Result.Reason : "OK");
        }
    }
}
