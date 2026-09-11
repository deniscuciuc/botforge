using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TeleForge.RateLimiting.Abstractions;

namespace TeleForge.RateLimiting.Redis;

/// <summary>
/// Redis-backed rate limit store using a sliding window counter implemented via Lua script.
/// Atomic operations ensure correctness under concurrent access across multiple instances.
/// </summary>
public class RedisRateLimitStore : IRateLimitStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisRateLimitStore> _logger;
    private readonly string _keyPrefix;

    // Lua script: sliding window rate limiter
    // Returns: [allowed (0/1), remaining permits, ttl in milliseconds]
    private static readonly string SlidingWindowScript = """
                                                         local key = KEYS[1]
                                                         local now = tonumber(ARGV[1])
                                                         local window_ms = tonumber(ARGV[2])
                                                         local max_permits = tonumber(ARGV[3])
                                                         local window_start = now - window_ms

                                                         -- Remove expired entries
                                                         redis.call('ZREMRANGEBYSCORE', key, '-inf', window_start)

                                                         -- Count current entries in window
                                                         local current = redis.call('ZCARD', key)

                                                         if current < max_permits then
                                                             -- Add new entry with current timestamp as score
                                                             redis.call('ZADD', key, now, now .. ':' .. math.random(1000000))
                                                             redis.call('PEXPIRE', key, window_ms)
                                                             return {1, max_permits - current - 1, 0}
                                                         else
                                                             -- Rate limited — calculate wait time until oldest entry expires
                                                             local oldest = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')
                                                             local wait_ms = 0
                                                             if #oldest >= 2 then
                                                                 wait_ms = tonumber(oldest[2]) + window_ms - now
                                                                 if wait_ms < 0 then wait_ms = 0 end
                                                             end
                                                             return {0, 0, wait_ms}
                                                         end
                                                         """;

    private readonly LoadedLuaScript? _loadedScript;

    public RedisRateLimitStore(
        IConnectionMultiplexer redis,
        ILogger<RedisRateLimitStore> logger,
        string keyPrefix = "tg:ratelimit:")
    {
        _redis = redis;
        _logger = logger;
        _keyPrefix = keyPrefix;

        try
        {
            var prepared = LuaScript.Prepare(SlidingWindowScript);
            _loadedScript = prepared.Load(_redis.GetServer(_redis.GetEndPoints()[0]));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not pre-load Lua script. Will use EVAL on each call.");
        }
    }

    public async Task<RateLimitResult> AcquireAsync(string key, RateLimitPolicy policy, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var db = _redis.GetDatabase();
        var redisKey = $"{_keyPrefix}{key}";
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowMs = (long)policy.Window.TotalMilliseconds;

        try
        {
            RedisResult result;
            if (_loadedScript != null)
                result = await db.ScriptEvaluateAsync(
                    _loadedScript.Hash,
                    [(RedisKey)redisKey],
                    [nowMs, windowMs, policy.PermitsPerWindow]).ConfigureAwait(false);
            else
                result = await db.ScriptEvaluateAsync(
                    SlidingWindowScript,
                    [(RedisKey)redisKey],
                    [nowMs, windowMs, policy.PermitsPerWindow]).ConfigureAwait(false);

            var values = (RedisResult[])result!;
            var allowed = (int)values[0] == 1;
            var remaining = (int)values[1];
            var waitMs = (long)values[2];

            return allowed
                ? RateLimitResult.Allowed(remaining)
                : RateLimitResult.Throttled(TimeSpan.FromMilliseconds(waitMs));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis rate limit check failed for key '{Key}'. Allowing request as fallback.", key);
            return RateLimitResult.Allowed();
        }
    }

    public async Task ResetAsync(string key, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var redisKey = $"{_keyPrefix}{key}";
        await db.KeyDeleteAsync(redisKey).ConfigureAwait(false);
    }
}
