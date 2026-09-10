using System.Collections.Concurrent;
using BotForge.Core;
using BotForge.Routing.Metrics;
using BotForge.Routing.Registration;
using BotForge.Routing.Routing;
using Microsoft.Extensions.Logging;

namespace BotForge.Routing.Middleware;

public class RateLimitMiddleware(IHandlerRegistry registry, ILogger<RateLimitMiddleware> logger)
    : ITelegramMiddleware
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastAccess = new();

    public async Task InvokeAsync(TelegramUpdateContext context, TelegramUpdateDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var rateLimitSeconds = ResolveRateLimit(context);
        var updateKind = ResolveUpdateKind(context);

        if (rateLimitSeconds <= 0 || context.UserId is null)
        {
            RoutingMetrics.RecordRateLimitCheck(updateKind, "skipped");
            await next(context).ConfigureAwait(false);
            return;
        }

        var key = BuildKey(context);
        var now = DateTimeOffset.UtcNow;

        if (_lastAccess.TryGetValue(key, out var lastTime))
        {
            var elapsed = now - lastTime;
            if (elapsed.TotalSeconds < rateLimitSeconds)
            {
                logger.LogDebug(
                    "Rate limited user {UserId}: {ElapsedMs}ms since last access, limit is {LimitSeconds}s",
                    context.UserId, elapsed.TotalMilliseconds, rateLimitSeconds);

                var retryAfter = TimeSpan.FromSeconds(rateLimitSeconds) - elapsed;
                RoutingMetrics.RecordRateLimitCheck(updateKind, "denied");
                RoutingMetrics.RecordRateLimitRetryAfter(updateKind, retryAfter);

                context.Result = UpdateResult.RetryLater(retryAfter);
                return;
            }
        }

        _lastAccess[key] = now;
        RoutingMetrics.RecordRateLimitCheck(updateKind, "allowed");
        await next(context).ConfigureAwait(false);
    }

    private int ResolveRateLimit(TelegramUpdateContext context)
    {
        var text = context.RawUpdate.Message?.Text;
        if (CommandTextParser.TryParse(text, out var cmd, out _))
        {
            return registry.FindCommand(cmd, context.BotId)?.RateLimitAttribute?.Seconds ?? 0;
        }

        var callbackData = context.RawUpdate.CallbackQuery?.Data;
        if (callbackData != null) return registry.FindCallback(callbackData)?.RateLimitAttribute?.Seconds ?? 0;

        return 0;
    }

    private static string ResolveUpdateKind(TelegramUpdateContext context)
    {
        var text = context.RawUpdate.Message?.Text;
        if (text != null && text.StartsWith('/'))
            return "command";

        return context.RawUpdate.CallbackQuery?.Data != null ? "callback" : "other";
    }

    private static string BuildKey(TelegramUpdateContext context)
    {
        var text = context.RawUpdate.Message?.Text;
        if (CommandTextParser.TryParse(text, out var cmd, out _))
            return $"cmd:{context.BotId}:{context.UserId}:{cmd}";

        var cbData = context.RawUpdate.CallbackQuery?.Data;
        return cbData != null
            ? $"cb:{context.BotId}:{context.UserId}:{cbData}"
            : $"other:{context.BotId}:{context.UserId}:{context.UpdateType}";
    }
}
