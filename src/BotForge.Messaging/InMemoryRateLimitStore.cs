using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using BotForge.RateLimiting.Abstractions;

namespace BotForge.Messaging;

public sealed class InMemoryRateLimitStore : IRateLimitStore, IDisposable
{
    private readonly ConcurrentDictionary<string, RateLimiter> _limiters = new();

    public async Task<RateLimitResult> AcquireAsync(string key, RateLimitPolicy policy, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var limiter = _limiters.GetOrAdd(key, _ => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = policy.PermitsPerWindow,
            TokensPerPeriod = policy.PermitsPerWindow,
            ReplenishmentPeriod = policy.Window,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            AutoReplenishment = true
        }));

        using var lease = await limiter.AcquireAsync(1, ct).ConfigureAwait(false);

        if (lease.IsAcquired)
            return RateLimitResult.Allowed();

        // Estimate wait time (estimation can be method)
        return RateLimitResult.Throttled(lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? retryAfter
            : policy.Window);
    }

    public Task ResetAsync(string key, CancellationToken ct = default)
    {
        if (_limiters.TryRemove(key, out var limiter))
            limiter.Dispose();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        foreach (var limiter in _limiters.Values)
            limiter.Dispose();

        _limiters.Clear();
        GC.SuppressFinalize(this);
    }
}
