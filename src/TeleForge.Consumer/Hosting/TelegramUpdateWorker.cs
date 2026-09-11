using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TeleForge.Consumer.Metrics;
using TeleForge.Core;

namespace TeleForge.Consumer.Hosting;

internal sealed class TelegramUpdateWorker(IServiceProvider services, ILogger<TelegramUpdateWorker> logger)
{
    public Task RunAsync(ChannelReader<TelegramUpdateEnvelope> reader, int concurrency, CancellationToken ct)
    {
        var tasks = new Task[concurrency];
        for (var i = 0; i < concurrency; i++)
        {
            var workerId = i;
            tasks[i] = Task.Run(() => ProcessUpdatesAsync(reader, workerId, ct), ct);
        }

        return Task.WhenAll(tasks);
    }

    private async Task ProcessUpdatesAsync(ChannelReader<TelegramUpdateEnvelope> reader, int workerId,
        CancellationToken ct)
    {
        logger.LogDebug("Update worker {WorkerId} started", workerId);
        ConsumerMetrics.WorkerStarted();

        try
        {
            await foreach (var envelope in reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                var sw = Stopwatch.StartNew();
                var status = "success";

                try
                {
                    var queueLag = DateTimeOffset.UtcNow - envelope.EnqueuedAt;
                    ConsumerMetrics.RecordQueueLag(envelope.BotId, envelope.RawUpdate.Type.ToString(), queueLag);

                    var updateType = envelope.RawUpdate.Type.ToString();
                    var botId = envelope.BotId;

                    logger.LogInformation(
                        "Worker {WorkerId}: processing update id={UpdateId} type={UpdateType} bot={BotId}",
                        workerId, envelope.RawUpdate.Id, updateType, botId);

                    var scope = services.CreateAsyncScope();
                    await using var scopeDisposal = scope.ConfigureAwait(false);
                    var pipeline = scope.ServiceProvider.GetRequiredService<ITelegramUpdatePipeline>();
                    var context = new TelegramUpdateContext(envelope.RawUpdate, botId)
                    {
                        RequestServices = scope.ServiceProvider
                    };

                    await pipeline.ProcessAsync(context, ct).ConfigureAwait(false);
                    status = context.Result?.Success == false ? "failed" : "success";
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    status = "exception";
                    logger.LogError(ex,
                        "Worker {WorkerId}: unhandled exception processing update {UpdateId} for bot '{BotId}'",
                        workerId, envelope.RawUpdate.Id, envelope.BotId);
                }
                finally
                {
                    sw.Stop();
                    ConsumerMetrics.RecordProcessed(envelope.BotId, envelope.RawUpdate.Type.ToString(), status,
                        sw.Elapsed);
                }
            }
        }
        finally
        {
            ConsumerMetrics.WorkerStopped();
            logger.LogDebug("Update worker {WorkerId} stopped", workerId);
        }
    }
}
