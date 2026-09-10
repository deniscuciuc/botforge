using BotForge.Integration.Tests.Fixtures;

namespace BotForge.Integration.Tests;

[Trait("Category", "Integration")]
public class RedisRateLimitTests : IAsyncLifetime
{
    private readonly RedisFixture _redis = new();

    public async Task InitializeAsync()
    {
        await _redis.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _redis.DisposeAsync();
    }

    [Fact]
    public async Task RedisContainer_StartsSuccessfully()
    {
        Assert.NotNull(_redis.ConnectionString);
    }

    [Fact]
    public async Task RedisContainer_AcceptsConnections()
    {
        var redis = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(_redis.ConnectionString);
        var db = redis.GetDatabase();

        await db.StringSetAsync("test:key", "hello");
        var value = await db.StringGetAsync("test:key");

        Assert.Equal("hello", value.ToString());
        await redis.DisposeAsync();
    }
}
