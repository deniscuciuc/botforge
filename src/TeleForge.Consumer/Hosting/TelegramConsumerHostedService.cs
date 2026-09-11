using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeleForge.Consumer.Polling;
using TeleForge.Core;

namespace TeleForge.Consumer.Hosting;

public class TelegramConsumerHostedService : BackgroundService
{
    private readonly TelegramConsumerOptions _options;
    private readonly ITelegramBotClientProvider _clientProvider;
    private readonly UpdateChannel _channel;
    private readonly TelegramUpdateWorker _worker;
    private readonly LongPollingConsumer _pollingConsumer;
    private readonly ILogger<TelegramConsumerHostedService> _logger;

    internal TelegramConsumerHostedService(
        TelegramConsumerOptions options,
        ITelegramBotClientProvider clientProvider,
        UpdateChannel channel,
        TelegramUpdateWorker worker,
        LongPollingConsumer pollingConsumer,
        ILogger<TelegramConsumerHostedService> logger)
    {
        _options = options;
        _clientProvider = clientProvider;
        _channel = channel;
        _worker = worker;
        _pollingConsumer = pollingConsumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telegram consumer starting. Transport: {Transport}, Concurrency: {Concurrency}",
            _options.DefaultTransport, _options.ConcurrencyLimit);

        var tasks = new List<Task>
        {
            _worker.RunAsync(_channel.Reader, _options.ConcurrencyLimit, stoppingToken)
        };

        var botIds = _clientProvider.GetRegisteredBotKeys();
        tasks.AddRange(from botId in botIds
                       let config = _clientProvider.GetConfiguration(botId)
                       where config.Transport == UpdateTransport.LongPolling
                       select _pollingConsumer.StartAsync(botId, _channel, stoppingToken));

        _logger.LogInformation("Telegram consumer started with {BotCount} bot(s)", botIds.Count);

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during shutdown, no action needed
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Telegram consumer shutting down. Draining pending updates...");

        _channel.Writer.Complete();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.GracefulShutdownTimeout);

        try
        {
            await base.StopAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Graceful shutdown timed out after {Timeout}", _options.GracefulShutdownTimeout);
        }

        _logger.LogInformation("Telegram consumer stopped");
    }
}
