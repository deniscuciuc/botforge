using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TeleForge.RateLimiting.Abstractions;

namespace TeleForge.RateLimiting.Redis;

public static class RedisRateLimitingExtensions
{
    /// <summary>
    /// Registers Redis as the rate limit store backend, replacing the default in-memory store.
    /// </summary>
    public static IServiceCollection AddRedisRateLimitStore(
        this IServiceCollection services,
        string keyPrefix = "tg:ratelimit:")
    {
        services.Replace(ServiceDescriptor.Singleton<IRateLimitStore>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            var logger = sp.GetRequiredService<ILogger<RedisRateLimitStore>>();
            return new RedisRateLimitStore(redis, logger, keyPrefix);
        }));

        return services;
    }
}
