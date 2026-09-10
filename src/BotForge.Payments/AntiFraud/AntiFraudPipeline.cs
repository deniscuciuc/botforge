using BotForge.Payments.Abstractions;
using Microsoft.Extensions.Logging;

namespace BotForge.Payments.AntiFraud;

/// <summary>
/// Composable pipeline that runs all registered <see cref="IAntiFraudCheck"/> instances.
/// </summary>
public class AntiFraudPipeline(
    IEnumerable<IAntiFraudCheck> checks,
    IPaymentMetrics metrics,
    ILogger<AntiFraudPipeline> logger)
{
    public async Task<AntiFraudResult> EvaluateAsync(AntiFraudContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var check in checks)
        {
            var result = await check.EvaluateAsync(context, ct).ConfigureAwait(false);
            metrics.AntiFraudCheck(check.Name, result.Passed);

            if (!result.Passed)
            {
                logger.LogWarning(
                    "Anti-fraud check {Check} failed for user {UserId}: {Reason}",
                    check.Name, context.UserId, result.FailureReason);
                return result;
            }

            logger.LogDebug("Anti-fraud check {Check} passed for user {UserId}", check.Name, context.UserId);
        }

        return AntiFraudResult.Pass();
    }
}
