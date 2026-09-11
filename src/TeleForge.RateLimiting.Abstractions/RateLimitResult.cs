namespace TeleForge.RateLimiting.Abstractions;

public class RateLimitResult
{
    public bool IsAllowed { get; init; }
    public TimeSpan? WaitTime { get; init; }
    public int RemainingPermits { get; init; }

    public static RateLimitResult Allowed(int remaining = 0)
    {
        return new RateLimitResult { IsAllowed = true, RemainingPermits = remaining };
    }

    public static RateLimitResult Throttled(TimeSpan waitTime)
    {
        return new RateLimitResult { IsAllowed = false, WaitTime = waitTime };
    }
}

public class RateLimitPolicy
{
    public string Key { get; init; } = null!;
    public int PermitsPerWindow { get; init; }
    public TimeSpan Window { get; init; }
}
