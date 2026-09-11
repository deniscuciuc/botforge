using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TeleForge.Hosting;

namespace TeleForge.Integration.Tests.Fixtures;

/// <summary>
/// Boots a Monolith host (Ingress + Sender in-process) for integration testing.
/// Uses WireMock as the Telegram API backend.
/// </summary>
public class MonolithServiceFixture(
    TelegramApiMockFixture apiMock,
    string? rabbitMqConnectionString = null)
    : IAsyncLifetime
{
    private IHost? _host;

    public IServiceProvider Services => _host!.Services;
    public TelegramApiMockFixture ApiMock => apiMock;

    public async Task InitializeAsync()
    {
        var config = new Dictionary<string, string?>
        {
            ["Telegram:Bots:0:Key"] = "test",
            ["Telegram:Bots:0:Token"] = "test-token",
            ["Telegram:Bots:0:Transport"] = "LongPolling",
            ["Telegram:Consumer:ConcurrencyLimit"] = "5",
            ["Telegram:Consumer:ChannelCapacity"] = "100",
            ["Telegram:Observability:EnableMetrics"] = "false",
            ["Telegram:Observability:EnablePrometheusExporter"] = "false"
        };

        if (!string.IsNullOrEmpty(rabbitMqConnectionString))
        {
            config["Telegram:Queue:Backend"] = "RabbitMq";
            config["Telegram:Queue:RabbitMqConnectionString"] = rabbitMqConnectionString;
        }
        else
        {
            config["Telegram:Queue:Backend"] = "InMemory";
        }

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(config);
        builder.Services.AddTelegramMonolithHost(builder.Configuration);

        _host = builder.Build();
        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
    }
}
