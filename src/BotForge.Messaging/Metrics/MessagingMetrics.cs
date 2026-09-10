using System.Diagnostics;
using System.Diagnostics.Metrics;
using BotForge.Core;

namespace BotForge.Messaging.Metrics;

internal static class MessagingMetrics
{
    private const string MeterName = "BotForge.Messaging";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> SendRequests =
        Meter.CreateCounter<long>("botforge.telegram.messaging.send.requests", "{request}",
            "Total send requests processed by the messaging pipeline.");

    private static readonly Histogram<double> SendDurationMs =
        Meter.CreateHistogram<double>("botforge.telegram.messaging.send.duration", "ms",
            "End-to-end send duration in milliseconds.");

    private static readonly Counter<long> RateLimitAcquire =
        Meter.CreateCounter<long>("botforge.telegram.messaging.ratelimit.acquire", "{attempt}",
            "Rate limit acquire attempts by scope and outcome.");

    private static readonly Histogram<double> RateLimitWaitMs =
        Meter.CreateHistogram<double>("botforge.telegram.messaging.ratelimit.wait.duration", "ms",
            "Delay duration introduced by rate limiting.");

    private static readonly Counter<long> RetryAttempts =
        Meter.CreateCounter<long>("botforge.telegram.messaging.retry.attempts", "{attempt}",
            "Number of retry attempts performed by retry middleware.");

    private static readonly Counter<long> CircuitTransitions =
        Meter.CreateCounter<long>("botforge.telegram.messaging.circuitbreaker.transitions", "{transition}",
            "Circuit breaker state transitions.");

    private static readonly Counter<long> CircuitRejected =
        Meter.CreateCounter<long>("botforge.telegram.messaging.circuitbreaker.rejections", "{request}",
            "Requests rejected because the circuit breaker is open.");

    public static void RecordSendResult(QueuedTelegramMessage message, SendResult result, TimeSpan elapsed)
    {
        if (!TelegramMetricsRuntime.MessagingEnabled)
            return;

        var operation = ResolveOperation(message);
        var status = result.Success ? "success" : "failure";

        var tags = new TagList
        {
            { "bot_key", message.BotId },
            { "operation", operation },
            { "status", status },
            { "error_code_class", ClassifyError(result.ErrorCode) }
        };

        SendRequests.Add(1, tags);
        SendDurationMs.Record(elapsed.TotalMilliseconds, tags);
    }

    public static void RecordSendException(QueuedTelegramMessage message, TimeSpan elapsed)
    {
        if (!TelegramMetricsRuntime.MessagingEnabled)
            return;

        var operation = ResolveOperation(message);
        var tags = new TagList
        {
            { "bot_key", message.BotId },
            { "operation", operation },
            { "status", "exception" },
            { "error_code_class", "exception" }
        };

        SendRequests.Add(1, tags);
        SendDurationMs.Record(elapsed.TotalMilliseconds, tags);
    }

    public static void RecordRateLimitAcquire(string botId, string scope, bool allowed)
    {
        if (!TelegramMetricsRuntime.MessagingEnabled)
            return;

        RateLimitAcquire.Add(1,
            new TagList
            {
                { "bot_key", botId },
                { "scope", scope },
                { "outcome", allowed ? "allowed" : "throttled" }
            });
    }

    public static void RecordRateLimitWait(string botId, string scope, TimeSpan wait)
    {
        if (!TelegramMetricsRuntime.MessagingEnabled)
            return;

        if (wait <= TimeSpan.Zero)
            return;

        RateLimitWaitMs.Record(wait.TotalMilliseconds,
            new TagList
            {
                { "bot_key", botId },
                { "scope", scope }
            });
    }

    public static void RecordRetryAttempts(string botId, int attempts)
    {
        if (!TelegramMetricsRuntime.MessagingEnabled)
            return;

        if (attempts <= 0)
            return;

        RetryAttempts.Add(attempts,
            new TagList
            {
                { "bot_key", botId }
            });
    }

    public static void RecordCircuitTransition(string botId, string transition)
    {
        if (!TelegramMetricsRuntime.MessagingEnabled)
            return;

        CircuitTransitions.Add(1,
            new TagList
            {
                { "bot_key", botId },
                { "state", transition }
            });
    }

    public static void RecordCircuitRejected(string botId)
    {
        if (!TelegramMetricsRuntime.MessagingEnabled)
            return;

        CircuitRejected.Add(1,
            new TagList
            {
                { "bot_key", botId }
            });
    }

    private static string ResolveOperation(QueuedTelegramMessage message)
    {
        if (message.EditMessageId.HasValue)
            return message.EditKeyboardOnly ? "edit_keyboard" : "edit_message";

        return message.MediaType.HasValue
            ? $"media_{message.MediaType.Value.ToString().ToLowerInvariant()}"
            : "send_message";
    }

    private static string ClassifyError(int? errorCode)
    {
        return errorCode switch
        {
            null => "none",
            429 => "429",
            >= 500 => "5xx",
            >= 400 => "4xx",
            _ => "other"
        };
    }
}
