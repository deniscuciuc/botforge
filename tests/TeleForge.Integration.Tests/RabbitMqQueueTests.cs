using TeleForge.Integration.Tests.Fixtures;

namespace TeleForge.Integration.Tests;

[Trait("Category", "Integration")]
public class RabbitMqQueueTests : IAsyncLifetime
{
    private readonly RabbitMqFixture _rabbitMq = new();
    private readonly TelegramApiMockFixture _apiMock = new();

    public async Task InitializeAsync()
    {
        await _rabbitMq.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        _apiMock.Dispose();
        await _rabbitMq.DisposeAsync();
    }

    [Fact]
    public async Task RabbitMqContainer_StartsSuccessfully()
    {
        Assert.NotNull(_rabbitMq.ConnectionString);
        Assert.Contains("amqp://", _rabbitMq.ConnectionString);
    }

    [Fact]
    public async Task MonolithWithRabbitMq_StartsSuccessfully()
    {
        await using var service = new MonolithServiceFixture(_apiMock, _rabbitMq.ConnectionString);
        await service.InitializeAsync();

        Assert.NotNull(service.Services);
    }
}
