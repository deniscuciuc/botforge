using BotForge.Core;
using BotForge.Messaging.Abstractions;
using BotForge.Messaging.Metrics;
using BotForge.RateLimiting.Abstractions;
using Microsoft.Extensions.Logging;

namespace BotForge.Messaging.Middleware;

public class RateLimitSendMiddleware(
    IRateLimitStore store,
    ILogger<RateLimitSendMiddleware> logger) : ISendMiddleware
{
    public async Task<SendResult> InvokeAsync(SendContext context, TelegramSendDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.Items.ContainsKey("BypassRateLimit"))
            return await next(context).ConfigureAwait(false);

        var msg = context.Message;
        var botConfig = context.Bot;

        // TODO: Refactor to check by method
        var globalResult = await store.AcquireAsync(
            $"bot:{msg.BotId}:global",
            new RateLimitPolicy
            {
                Key = $"bot:{msg.BotId}:global",
                PermitsPerWindow = botConfig.RateLimit.GlobalPerSecond,
                Window = TimeSpan.FromSeconds(1)
            },
            context.CancellationToken).ConfigureAwait(false);

        MessagingMetrics.RecordRateLimitAcquire(msg.BotId, "global", globalResult.IsAllowed);

        if (!globalResult.IsAllowed)
        {
            logger.LogDebug("Rate limited (global) for bot '{BotId}'. Wait {WaitMs}ms",
                msg.BotId, globalResult.WaitTime?.TotalMilliseconds);

            if (globalResult.WaitTime.HasValue)
            {
                MessagingMetrics.RecordRateLimitWait(msg.BotId, "global", globalResult.WaitTime.Value);
                await Task.Delay(globalResult.WaitTime.Value, context.CancellationToken).ConfigureAwait(false);
            }
        }

        // TODO: Refactor to check by method
        var chatResult = await store.AcquireAsync(
            $"bot:{msg.BotId}:chat:{msg.ChatId}",
            new RateLimitPolicy
            {
                Key = $"bot:{msg.BotId}:chat:{msg.ChatId}",
                PermitsPerWindow = botConfig.RateLimit.PerChatPerSecond,
                Window = TimeSpan.FromSeconds(1)
            },
            context.CancellationToken).ConfigureAwait(false);

        MessagingMetrics.RecordRateLimitAcquire(msg.BotId, "chat", chatResult.IsAllowed);

        if (chatResult.IsAllowed) return await next(context).ConfigureAwait(false);

        logger.LogDebug("Rate limited (per-chat) for chat {ChatId}. Wait {WaitMs}ms",
            msg.ChatId, chatResult.WaitTime?.TotalMilliseconds);

        if (!chatResult.WaitTime.HasValue) return await next(context).ConfigureAwait(false);

        MessagingMetrics.RecordRateLimitWait(msg.BotId, "chat", chatResult.WaitTime.Value);
        await Task.Delay(chatResult.WaitTime.Value, context.CancellationToken).ConfigureAwait(false);

        return await next(context).ConfigureAwait(false);
    }
}
