using TeleForge.RateLimiting.Abstractions;

namespace TeleForge.Messaging.Tests;

public class InMemoryRateLimitStoreTests : IDisposable
{
    private readonly InMemoryRateLimitStore _store = new();

    [Fact]
    public async Task AcquireAsync_FirstRequest_IsAllowed()
    {
        var policy = new RateLimitPolicy
        {
            Key = "test",
            PermitsPerWindow = 10,
            Window = TimeSpan.FromSeconds(1)
        };

        var result = await _store.AcquireAsync("test-key", policy);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task AcquireAsync_ExhaustsTokens_IsThrottled()
    {
        var policy = new RateLimitPolicy
        {
            Key = "test",
            PermitsPerWindow = 1,
            Window = TimeSpan.FromSeconds(10)
        };

        // First request should succeed
        var first = await _store.AcquireAsync("exhaust-key", policy);
        Assert.True(first.IsAllowed);

        // Second request should be throttled
        var second = await _store.AcquireAsync("exhaust-key", policy);
        Assert.False(second.IsAllowed);
        Assert.NotNull(second.WaitTime);
    }

    [Fact]
    public async Task ResetAsync_AllowsNewRequests()
    {
        var policy = new RateLimitPolicy
        {
            Key = "test",
            PermitsPerWindow = 1,
            Window = TimeSpan.FromSeconds(10)
        };

        await _store.AcquireAsync("reset-key", policy);
        await _store.AcquireAsync("reset-key", policy); // exhaust

        await _store.ResetAsync("reset-key");

        var result = await _store.AcquireAsync("reset-key", policy);
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task AcquireAsync_DifferentKeys_IndependentLimits()
    {
        var policy = new RateLimitPolicy
        {
            Key = "test",
            PermitsPerWindow = 1,
            Window = TimeSpan.FromSeconds(10)
        };

        var r1 = await _store.AcquireAsync("key-a", policy);
        var r2 = await _store.AcquireAsync("key-b", policy);

        Assert.True(r1.IsAllowed);
        Assert.True(r2.IsAllowed);
    }

    public void Dispose()
    {
        _store.Dispose();
    }
}
