using Microsoft.Extensions.Options;
using TeleForge.Payments.Abstractions;

namespace TeleForge.Payments.AntiFraud;

public class VelocityCheckOptions
{
    /// <summary>Maximum payouts a single user may request per rolling hour.</summary>
    public int MaxPerHour { get; set; } = 5;

    /// <summary>Maximum payouts a single user may request per rolling day.</summary>
    public int MaxPerDay { get; set; } = 20;
}

/// <summary>
/// Rejects a payout when the user has already requested too many within a rolling hour or
/// day, counted through <see cref="IPaymentStore.CountPayoutsSinceAsync"/>.
/// </summary>
/// <remarks>
/// If no <see cref="IPaymentStore"/> is registered this check <b>fails</b> rather than
/// passes. Registering a velocity check states an intent to limit payouts; silently
/// approving everything because the store is missing would be the worst possible outcome for
/// a control that exists to stop money leaving.
/// </remarks>
public class VelocityCheck(IOptions<VelocityCheckOptions> options, IPaymentStore? store = null)
    : IAntiFraudCheck
{
    private readonly VelocityCheckOptions _options = options.Value;
    private readonly IPaymentStore? _store = store;

    public string Name => "VelocityCheck";

    public async Task<AntiFraudResult> EvaluateAsync(AntiFraudContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_store is null)
            return AntiFraudResult.Fail(
                "VelocityCheck is registered but no IPaymentStore is available to count recent payouts.",
                "antifraud_velocity_unavailable");

        var now = DateTimeOffset.UtcNow;

        var lastHour = await _store.CountPayoutsSinceAsync(
            context.UserId, now.AddHours(-1), ct).ConfigureAwait(false);

        if (lastHour >= _options.MaxPerHour)
            return AntiFraudResult.Fail(
                $"User {context.UserId} has {lastHour} payouts in the last hour, limit is {_options.MaxPerHour}",
                "antifraud_velocity_hourly");

        var lastDay = await _store.CountPayoutsSinceAsync(
            context.UserId, now.AddDays(-1), ct).ConfigureAwait(false);

        if (lastDay >= _options.MaxPerDay)
            return AntiFraudResult.Fail(
                $"User {context.UserId} has {lastDay} payouts in the last day, limit is {_options.MaxPerDay}",
                "antifraud_velocity_daily");

        return AntiFraudResult.Pass();
    }
}
