using System.Threading.Channels;
using BotForge.Core;
using BotForge.Messaging.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BotForge.Messaging;

public sealed class ChannelQueueBackend(
    ISendPipeline sendPipeline,
    ITelegramBotClientProvider botProvider,
    ILogger<ChannelQueueBackend> logger,
    int capacity = 10_000,
    int consumerCount = 4) : IMessageQueueBackend, IHostedService, IDisposable
{
    private readonly Channel<QueuedTelegramMessage> _channel = Channel.CreateBounded<QueuedTelegramMessage>(
        new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = false
        });

    private readonly CancellationTokenSource _cts = new();
    private readonly List<Task> _consumers = [];

    public async Task EnqueueAsync(QueuedTelegramMessage message, CancellationToken ct = default)
    {
        await _channel.Writer.WriteAsync(message, ct).ConfigureAwait(false);
    }

    public async Task EnqueueBatchAsync(IEnumerable<QueuedTelegramMessage> messages, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        foreach (var message in messages)
            await _channel.Writer.WriteAsync(message, ct).ConfigureAwait(false);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < consumerCount; i++)
        {
            var consumerId = i;
            _consumers.Add(Task.Run(() => ConsumeAsync(consumerId, _cts.Token), _cts.Token));
        }

        logger.LogInformation("Started {Count} queue consumers", consumerCount);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping queue backend. Draining {Count} pending messages...",
            _channel.Reader.Count);

        _channel.Writer.Complete();
        await _cts.CancelAsync().ConfigureAwait(false);

        try
        {
            await Task.WhenAll(_consumers).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected if the shutdown timeout is reached, no action needed
        }

        logger.LogInformation("Queue backend stopped.");
    }

    /// <summary>
    /// Releases the cancellation source that signals the consumer loops. The host calls this
    /// after <see cref="StopAsync"/>; disposing without stopping first is safe but leaves the
    /// consumers to observe cancellation on their own.
    /// </summary>
    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task ConsumeAsync(int consumerId, CancellationToken ct)
    {
        logger.LogDebug("Queue consumer {ConsumerId} started", consumerId);

        try
        {
            await foreach (var message in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                try
                {
                    var botConfig = botProvider.GetConfiguration(message.BotId);
                    var context = new SendContext
                    {
                        Message = message,
                        Bot = botConfig,
                        CancellationToken = ct
                    };

                    await sendPipeline.SendAsync(context, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Queue consumer {ConsumerId} failed to process message {MessageId}",
                        consumerId, message.Id);
                }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown, no action needed
        }

        logger.LogDebug("Queue consumer {ConsumerId} stopped", consumerId);
    }
}
