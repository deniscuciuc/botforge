using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotForge.Payments.Fragment;

/// <summary>
/// Health check that verifies Fragment API reachability and wallet balance.
/// </summary>
internal sealed class FragmentHealthCheck(
    FragmentClient client,
    IOptions<FragmentOptions> options,
    ILogger<FragmentHealthCheck> logger)
    : IHealthCheck
{
    private readonly FragmentOptions _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var wallet = await client.GetWalletBalanceAsync(ct).ConfigureAwait(false);
            if (wallet is null)
                return HealthCheckResult.Unhealthy("Fragment API returned null wallet response");

            var data = new Dictionary<string, object>
            {
                ["balance"] = wallet.Balance
            };

            if (decimal.TryParse(wallet.Balance, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var balance))
            {
                data["balance_parsed"] = balance;

                if (_options.MinWalletBalance > 0 && balance < _options.MinWalletBalance)
                    return HealthCheckResult.Degraded(
                        $"Fragment wallet balance {balance} is below minimum {_options.MinWalletBalance}",
                        data: data);
            }

            return HealthCheckResult.Healthy("Fragment API reachable", data);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fragment health check failed");
            return HealthCheckResult.Unhealthy("Fragment API unreachable", ex);
        }
    }
}
