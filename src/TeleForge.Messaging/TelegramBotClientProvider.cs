using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TeleForge.Core;
using Telegram.Bot;

namespace TeleForge.Messaging;

public sealed class TelegramBotClientProvider(
    TelegramMessagingOptions options,
    IHttpClientFactory httpClientFactory,
    ILogger<TelegramBotClientProvider> logger)
    : ITelegramBotClientProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, ITelegramBotClient> _clients = new();
    private readonly ConcurrentDictionary<string, HttpClient> _httpClients = new();
    private readonly Dictionary<string, BotConfiguration> _configs = options.Bots;

    public ITelegramBotClient GetClient(string botId)
    {
        return _clients.GetOrAdd(botId, key =>
        {
            if (!_configs.TryGetValue(key, out var config))
                throw new InvalidOperationException($"Bot '{key}' is not registered.");

            HttpClient httpClient;

            if (config.DedicatedTransport)
            {
                var handler = new SocketsHttpHandler
                {
                    MaxConnectionsPerServer = config.TransportOptions.MaxConnections,
                    ConnectTimeout = config.TransportOptions.ConnectTimeout,
                    PooledConnectionLifetime = config.TransportOptions.ConnectionLifetime,
                    EnableMultipleHttp2Connections = false
                };

                httpClient = new HttpClient(handler, true)
                {
                    Timeout = config.TransportOptions.RequestTimeout
                };
                _httpClients[key] = httpClient;
            }
            else
            {
                httpClient = httpClientFactory.CreateClient($"TelegramBot_{key}");
                httpClient.Timeout = config.TransportOptions.RequestTimeout;
            }

            logger.LogInformation(
                "Created Telegram bot client for '{BotId}' (dedicated={Dedicated})",
                key, config.DedicatedTransport);

            return new TelegramBotClient(config.Token, httpClient);
        });
    }

    public BotConfiguration GetConfiguration(string botId)
    {
        return _configs.TryGetValue(botId, out var config)
            ? config
            : throw new InvalidOperationException($"Bot '{botId}' is not registered.");
    }

    public IReadOnlyList<string> GetRegisteredBotKeys()
    {
        return _configs.Keys.ToList();
    }

    public void Dispose()
    {
        foreach (var httpClient in _httpClients.Values)
            httpClient.Dispose();

        _httpClients.Clear();
        _clients.Clear();
        GC.SuppressFinalize(this);
    }
}
