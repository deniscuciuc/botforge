using BotForge.Hosting;
using BotForge.Integration.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BotForge.Integration.Tests;

[Trait("Category", "Integration")]
public class MultiBotTests : IDisposable
{
    private readonly TelegramApiMockFixture _apiMock = new();

    [Fact]
    public async Task MonolithHost_WithMultipleBots_StartsSuccessfully()
    {
        var config = new Dictionary<string, string?>
        {
            ["Telegram:Bots:0:Key"] = "main",
            ["Telegram:Bots:0:Token"] = "token-main",
            ["Telegram:Bots:0:Transport"] = "LongPolling",
            ["Telegram:Bots:1:Key"] = "notifications",
            ["Telegram:Bots:1:Token"] = "token-notifications",
            ["Telegram:Bots:1:Transport"] = "LongPolling",
            ["Telegram:Consumer:ConcurrencyLimit"] = "5",
            ["Telegram:Observability:EnableMetrics"] = "false",
            ["Telegram:Queue:Backend"] = "InMemory"
        };

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(config);
        builder.Services.AddTelegramMonolithHost(builder.Configuration);

        using var host = builder.Build();
        await host.StartAsync();

        // Verify both bots are registered
        var provider = host.Services.GetRequiredService<Core.ITelegramBotClientProvider>();
        var keys = provider.GetRegisteredBotKeys();

        Assert.Contains("main", keys);
        Assert.Contains("notifications", keys);

        await host.StopAsync();
    }

    public void Dispose()
    {
        _apiMock.Dispose();
        GC.SuppressFinalize(this);
    }
}
