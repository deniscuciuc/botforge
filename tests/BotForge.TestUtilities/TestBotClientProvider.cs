using BotForge.Core;
using Telegram.Bot;

namespace BotForge.TestUtilities;

public class TestBotClientProvider : ITelegramBotClientProvider
{
    private readonly Dictionary<string, (ITelegramBotClient Client, BotConfiguration Config)> _bots = new();

    public ITelegramBotClient RegisterBot(string botId, Action<BotConfiguration>? configure = null)
    {
        var config = new BotConfiguration
        {
            Key = botId,
            Token = $"fake-token-{botId}"
        };
        configure?.Invoke(config);

        var client = FakeTelegramBotClientFactory.Create();
        _bots[botId] = (client, config);
        return client;
    }

    public ITelegramBotClient GetClient(string botId)
    {
        return _bots.TryGetValue(botId, out var entry)
            ? entry.Client
            : throw new KeyNotFoundException($"Bot '{botId}' not registered.");
    }

    public BotConfiguration GetConfiguration(string botId)
    {
        return _bots.TryGetValue(botId, out var entry)
            ? entry.Config
            : throw new KeyNotFoundException($"Bot '{botId}' not registered.");
    }

    public IReadOnlyList<string> GetRegisteredBotKeys()
    {
        return _bots.Keys.ToList();
    }
}
