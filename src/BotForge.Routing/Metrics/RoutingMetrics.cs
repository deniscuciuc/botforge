using System.Diagnostics;
using System.Diagnostics.Metrics;
using BotForge.Core;

namespace BotForge.Routing.Metrics;

internal static class RoutingMetrics
{
    private const string MeterName = "BotForge.Routing";

    private static readonly Meter Meter = new(MeterName);

    // ── Pipeline-level metrics ───────────────────────────────────────────

    private static readonly Counter<long> UpdatesTotal =
        Meter.CreateCounter<long>("botforge.telegram.routing.updates.total", "{update}",
            "Total updates processed by the routing pipeline.");

    private static readonly Histogram<double> UpdateDurationMs =
        Meter.CreateHistogram<double>("botforge.telegram.routing.updates.duration", "ms",
            "Routing pipeline execution duration in milliseconds.");

    private static readonly Counter<long> RateLimitChecks =
        Meter.CreateCounter<long>("botforge.telegram.routing.ratelimit.checks", "{check}",
            "Routing rate-limit checks by kind and outcome.");

    private static readonly Histogram<double> RateLimitRetryAfterMs =
        Meter.CreateHistogram<double>("botforge.telegram.routing.ratelimit.retry_after", "ms",
            "Retry-after durations returned by routing rate limiting.");

    // ── Bot query (handler-level) latency tracking ───────────────────────
    //
    // These metrics provide per-handler-query observability so operators can
    // identify slow commands, callback patterns, or text handlers without
    // application-level instrumentation.
    //
    // Cardinality rules:
    //   - "query" label: bounded to known command names, callback patterns,
    //     or a short handler-type name for unstructured handlers.
    //   - "query_type" label: one of "command", "callback_query",
    //     "text_message", "inline_query", "media", "location", etc.
    //   - Free-form user text (chat_id, user_id, raw callback payload) is
    //     NEVER included as a label.

    /// <summary>Per-handler execution count, labelled by bot, query type, and query identifier.</summary>
    private static readonly Counter<long> HandlerCalls =
        Meter.CreateCounter<long>("botforge.telegram.routing.handler.calls", "{call}",
            "Per-handler execution count by query type.");

    /// <summary>Per-handler execution duration, labelled by bot, query type, and query identifier.</summary>
    private static readonly Histogram<double> HandlerLatencyMs =
        Meter.CreateHistogram<double>("botforge.telegram.routing.handler.latency", "ms",
            "Per-handler execution duration by query type.");

    public static void RecordUpdate(string botId, string updateType, string status, TimeSpan elapsed)
    {
        if (!TelegramMetricsRuntime.RoutingEnabled)
            return;

        var tags = new TagList
        {
            { "bot_key", botId },
            { "update_type", updateType },
            { "status", status }
        };

        UpdatesTotal.Add(1, tags);
        UpdateDurationMs.Record(elapsed.TotalMilliseconds, tags);
    }

    public static void RecordRateLimitCheck(string updateKind, string outcome)
    {
        if (!TelegramMetricsRuntime.RoutingEnabled)
            return;

        RateLimitChecks.Add(1,
            new TagList
            {
                { "update_kind", updateKind },
                { "outcome", outcome }
            });
    }

    public static void RecordRateLimitRetryAfter(string updateKind, TimeSpan retryAfter)
    {
        if (!TelegramMetricsRuntime.RoutingEnabled)
            return;

        if (retryAfter <= TimeSpan.Zero)
            return;

        RateLimitRetryAfterMs.Record(retryAfter.TotalMilliseconds,
            new TagList
            {
                { "update_kind", updateKind }
            });
    }

    // ── Bot query latency helpers ────────────────────────────────────────

    /// <summary>
    ///     Records a handler execution. The <paramref name="query"/> label must be
    ///     a known bounded value — command name, callback pattern prefix, or handler type name.
    /// </summary>
    public static void RecordHandlerCall(string botId, string queryType, string query, TimeSpan elapsed, string status)
    {
        if (!TelegramMetricsRuntime.RoutingEnabled)
            return;

        var tags = new TagList
        {
            { "bot_key", botId },
            { "query_type", queryType },
            { "query", query },
            { "status", status }
        };

        HandlerCalls.Add(1, tags);
        HandlerLatencyMs.Record(elapsed.TotalMilliseconds, tags);
    }
}
