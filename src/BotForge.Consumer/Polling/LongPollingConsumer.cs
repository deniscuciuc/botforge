using BotForge.Consumer.Hosting;
using BotForge.Consumer.Metrics;
using BotForge.Core;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace BotForge.Consumer.Polling;

internal sealed class LongPollingConsumer(
    ITelegramBotClientProvider clientProvider,
    TelegramConsumerOptions options,
    ILogger<LongPollingConsumer> logger)
{
    public async Task StartAsync(
        string botId,
        UpdateChannel channel,
        CancellationToken ct)
    {
        var client = clientProvider.GetClient(botId);
        var config = clientProvider.GetConfiguration(botId);

        var allowedUpdates = config.AllowedUpdates.Length > 0 ? config.AllowedUpdates : null;

        var updatesList = allowedUpdates is not null
            ? string.Join(", ", allowedUpdates.Select(u => u.ToString()))
            : "null (all)";
        logger.LogInformation(
            "Starting long polling for bot '{BotId}' with allowed_updates=[{Updates}]",
            botId, updatesList);

        var tcs = new TaskCompletionSource();
        var reg = ct.Register(() => tcs.TrySetResult());
        await using var regDisposal = reg.ConfigureAwait(false);

        client.StartReceiving(
            async (_, update, token) =>
            {
                ConsumerMetrics.RecordIngested(botId, "polling", update.Type.ToString());
                await channel.Writer.WriteAsync(
                    new TelegramUpdateEnvelope(botId, update, DateTimeOffset.UtcNow), token).ConfigureAwait(false);
            },
            (_, exception, _) =>
            {
                if (ct.IsCancellationRequested || IsExpectedPollingInterruption(exception))
                {
                    logger.LogDebug(exception, "Long polling interrupted for bot '{BotId}'", botId);
                    return Task.CompletedTask;
                }

                logger.LogError(exception, "Polling error for bot '{BotId}'", botId);
                return Task.CompletedTask;
            },
            new ReceiverOptions
            {
                AllowedUpdates = allowedUpdates,
                Limit = options.PollingLimit
            },
            ct);

        if (logger.IsEnabled(LogLevel.Information))
        {
            var me = await client.GetMe(ct).ConfigureAwait(false);
            logger.LogInformation("Bot '{BotId}' polling as @{Username}", botId, me.Username);
        }

        await tcs.Task.ConfigureAwait(false);
    }

    private static bool IsExpectedPollingInterruption(Exception exception)
    {
        if (exception is OperationCanceledException or TaskCanceledException)
            return true;

        return exception.InnerException is OperationCanceledException
            or TaskCanceledException
            or TimeoutException;
    }
}
