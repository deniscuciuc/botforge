namespace TeleForge.RateLimiting.Abstractions;

public interface IRateLimitStore
{
    Task<RateLimitResult> AcquireAsync(string key, RateLimitPolicy policy, CancellationToken ct = default);
    Task ResetAsync(string key, CancellationToken ct = default);
}
