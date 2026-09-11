using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TeleForge.Core;
using TeleForge.Routing.Metrics;

namespace TeleForge.Routing.Middleware;

public class MetricsMiddleware(ILogger<MetricsMiddleware> logger) : ITelegramMiddleware
{
    private static long _totalUpdates;
    private static long _failedUpdates;

    public async Task InvokeAsync(TelegramUpdateContext context, TelegramUpdateDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        Interlocked.Increment(ref _totalUpdates);
        var sw = Stopwatch.StartNew();
        var hadException = false;

        try
        {
            await next(context).ConfigureAwait(false);

            if (context.Result is { Success: false })
                Interlocked.Increment(ref _failedUpdates);
        }
        catch
        {
            hadException = true;
            Interlocked.Increment(ref _failedUpdates);
            throw;
        }
        finally
        {
            sw.Stop();

            var status = hadException
                ? "exception"
                : context.Result?.Success == false
                    ? "failed"
                    : "success";
            RoutingMetrics.RecordUpdate(context.BotId, context.UpdateType, status, sw.Elapsed);

            logger.LogInformation(
                "metrics.pipeline update_type={UpdateType} duration_ms={DurationMs} total={Total} failed={Failed}",
                context.UpdateType, sw.ElapsedMilliseconds,
                Interlocked.Read(ref _totalUpdates), Interlocked.Read(ref _failedUpdates));
        }
    }

    public static (long Total, long Failed) GetCounts()
    {
        return (Interlocked.Read(ref _totalUpdates), Interlocked.Read(ref _failedUpdates));
    }
}
