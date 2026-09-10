using System.Diagnostics;
using System.Diagnostics.Metrics;
using BotForge.Core;

namespace BotForge.Consumer.Metrics;

internal static class ConsumerMetrics
{
    private const string MeterName = "BotForge.Consumer";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> UpdatesIngested =
        Meter.CreateCounter<long>("botforge.telegram.consumer.updates.ingested", "{update}",
            "Updates ingested from polling or webhook transports.");

    private static readonly Counter<long> UpdatesProcessed =
        Meter.CreateCounter<long>("botforge.telegram.consumer.updates.processed", "{update}",
            "Updates processed by worker pipelines.");

    private static readonly Histogram<double> ProcessingDurationMs =
        Meter.CreateHistogram<double>("botforge.telegram.consumer.updates.processing.duration", "ms",
            "Worker processing duration per update.");

    private static readonly Histogram<double> QueueLagMs =
        Meter.CreateHistogram<double>("botforge.telegram.consumer.updates.queue_lag", "ms",
            "Time spent waiting in the update queue before processing.");

    private static readonly UpDownCounter<long> ActiveWorkers =
        Meter.CreateUpDownCounter<long>("botforge.telegram.consumer.workers.active", "{worker}",
            "Currently active consumer workers.");

    public static void RecordIngested(string botId, string transport, string updateType)
    {
        if (!TelegramMetricsRuntime.ConsumerEnabled)
            return;

        UpdatesIngested.Add(1,
            new TagList
            {
                { "bot_key", botId },
                { "transport", transport },
                { "update_type", updateType }
            });
    }

    public static void RecordProcessed(string botId, string updateType, string status, TimeSpan duration)
    {
        if (!TelegramMetricsRuntime.ConsumerEnabled)
            return;

        var tags = new TagList
        {
            { "bot_key", botId },
            { "update_type", updateType },
            { "status", status }
        };

        UpdatesProcessed.Add(1, tags);
        ProcessingDurationMs.Record(duration.TotalMilliseconds, tags);
    }

    public static void RecordQueueLag(string botId, string updateType, TimeSpan lag)
    {
        if (!TelegramMetricsRuntime.ConsumerEnabled)
            return;

        if (lag < TimeSpan.Zero)
            lag = TimeSpan.Zero;

        QueueLagMs.Record(lag.TotalMilliseconds,
            new TagList
            {
                { "bot_key", botId },
                { "update_type", updateType }
            });
    }

    public static void WorkerStarted()
    {
        if (!TelegramMetricsRuntime.ConsumerEnabled)
            return;

        ActiveWorkers.Add(1);
    }

    public static void WorkerStopped()
    {
        if (!TelegramMetricsRuntime.ConsumerEnabled)
            return;

        ActiveWorkers.Add(-1);
    }
}
