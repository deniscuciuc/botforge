using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TeleForge.Core;
using TeleForge.Messaging.Abstractions;
using TeleForge.Messaging.Metrics;

namespace TeleForge.Messaging.Middleware;

public class MetricsSendMiddleware(ILogger<MetricsSendMiddleware> logger) : ISendMiddleware
{
    public async Task<SendResult> InvokeAsync(SendContext context, TelegramSendDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var msg = context.Message;
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await next(context).ConfigureAwait(false);
            sw.Stop();

            MessagingMetrics.RecordSendResult(msg, result, sw.Elapsed);

            logger.LogDebug(
                "Message sent to {ChatId} via bot '{BotId}' in {ElapsedMs}ms. Success={Success}",
                msg.ChatId, msg.BotId, sw.ElapsedMilliseconds, result.Success);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            MessagingMetrics.RecordSendException(msg, sw.Elapsed);
            logger.LogError(ex,
                "Message send failed for {ChatId} via bot '{BotId}' after {ElapsedMs}ms",
                msg.ChatId, msg.BotId, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
