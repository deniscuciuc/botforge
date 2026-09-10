using Telegram.Bot.Types.Enums;

namespace BotForge.Core.Tests;

public class BotConfigurationTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var config = new BotConfiguration();

        Assert.Equal(50, config.ConcurrencyLimit);
        Assert.Equal(UpdateTransport.LongPolling, config.Transport);
        Assert.Empty(config.AllowedUpdates);
        Assert.False(config.DedicatedTransport);
    }

    [Fact]
    public void RateLimitDefaults_AreCorrect()
    {
        var rateLimit = new BotRateLimitOptions();

        Assert.Equal(30, rateLimit.GlobalPerSecond);
        Assert.Equal(1, rateLimit.PerChatPerSecond);
        Assert.Equal(20, rateLimit.GroupPerMinute);
    }

    [Fact]
    public void TransportOptions_HaveSensibleDefaults()
    {
        var options = new BotTransportOptions();

        Assert.Equal(8, options.MaxConnections);
        Assert.Equal(TimeSpan.FromSeconds(5), options.ConnectTimeout);
        Assert.Equal(TimeSpan.FromSeconds(30), options.RequestTimeout);
        Assert.Equal(TimeSpan.FromMinutes(5), options.ConnectionLifetime);
    }

    [Fact]
    public void Properties_AreSettable()
    {
        var config = new BotConfiguration
        {
            Key = "mybot",
            Token = "123:ABC",
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
            DedicatedTransport = true,
            ConcurrencyLimit = 100,
            Transport = UpdateTransport.Webhook
        };

        Assert.Equal("mybot", config.Key);
        Assert.Equal("123:ABC", config.Token);
        Assert.Equal(2, config.AllowedUpdates.Length);
        Assert.True(config.DedicatedTransport);
        Assert.Equal(100, config.ConcurrencyLimit);
        Assert.Equal(UpdateTransport.Webhook, config.Transport);
    }
}
