using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using TeleForge.Core;
using Telegram.Bot;

namespace TeleForge.Consumer.Health;

public class TelegramBotHealthCheck(
    ITelegramBotClientProvider clientProvider,
    string botId,
    ILogger<TelegramBotHealthCheck> logger)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientProvider.GetClient(botId);
            var me = await client.GetMe(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy($"Bot @{me.Username} is reachable");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Health check failed for bot '{BotId}'", botId);
            return HealthCheckResult.Unhealthy($"Bot '{botId}' is unreachable", ex);
        }
    }
}
