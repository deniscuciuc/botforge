using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TeleForge.Core;
using TeleForge.Messaging.Abstractions;
using TeleForge.Messaging.Metrics;

namespace TeleForge.Messaging.Middleware;

public class CircuitBreakerSendMiddleware(
    TelegramMessagingOptions options,
    ILogger<CircuitBreakerSendMiddleware> logger)
    : ISendMiddleware
{
    private readonly ConcurrentDictionary<string, CircuitState> _states = new();

    public async Task<SendResult> InvokeAsync(SendContext context, TelegramSendDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var botId = context.Message.BotId;
        var state = _states.GetOrAdd(botId, _ => new CircuitState());

        if (state.IsOpen)
        {
            if (DateTimeOffset.UtcNow >= state.OpenUntil)
            {
                logger.LogInformation("Circuit breaker half-open for bot '{BotId}'. Probing...", botId);
                state.IsHalfOpen = true;
                MessagingMetrics.RecordCircuitTransition(botId, "half_open");
            }
            else
            {
                logger.LogWarning("Circuit breaker open for bot '{BotId}'. Rejecting request.", botId);
                MessagingMetrics.RecordCircuitRejected(botId);
                return SendResult.Failed($"Circuit breaker open for bot '{botId}'", 503);
            }
        }

        var result = await next(context).ConfigureAwait(false);

        if (result.Success)
        {
            if (state.IsHalfOpen || state.IsOpen)
            {
                logger.LogInformation("Circuit breaker closed for bot '{BotId}'.", botId);
                state.Reset();
                MessagingMetrics.RecordCircuitTransition(botId, "closed");
            }
            else
            {
                Interlocked.Exchange(ref state.ConsecutiveFailures, 0);
            }
        }
        else
        {
            var failures = Interlocked.Increment(ref state.ConsecutiveFailures);

            if (failures < options.CircuitBreaker.FailureThreshold) return result;

            // Use Retry-After from the API response when configured, otherwise static BreakDuration
            var breakDuration = options.CircuitBreaker.UseRetryAfterForBreakDuration &&
                                result.RetryAfter is { } retryAfter
                ? retryAfter
                : options.CircuitBreaker.BreakDuration;

            state.IsOpen = true;
            state.IsHalfOpen = false;
            state.OpenUntil = DateTimeOffset.UtcNow + breakDuration;

            logger.LogError(
                "Circuit breaker opened for bot '{BotId}' after {Failures} consecutive failures. Open until {OpenUntil} (break={BreakDuration}s)",
                botId, failures, state.OpenUntil, breakDuration.TotalSeconds);
            MessagingMetrics.RecordCircuitTransition(botId, "open");
        }

        return result;
    }

    private sealed class CircuitState
    {
        public int ConsecutiveFailures;
        public volatile bool IsOpen;
        public volatile bool IsHalfOpen;
        public DateTimeOffset OpenUntil;

        public void Reset()
        {
            Interlocked.Exchange(ref ConsecutiveFailures, 0);
            IsOpen = false;
            IsHalfOpen = false;
            OpenUntil = default;
        }
    }
}
