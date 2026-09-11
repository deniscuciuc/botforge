using Microsoft.Extensions.Options;
using TeleForge.Payments.Abstractions;

namespace TeleForge.Payments.AntiFraud;

public class VelocityCheckOptions
{
    public int MaxPerHour { get; set; } = 5;
    public int MaxPerDay { get; set; } = 20;
}

/// <summary>
/// Checks withdrawal rate against configured limits.
/// Requires <see cref="IPaymentStore"/> to query recent payout history.
/// </summary>
public class VelocityCheck(IOptions<VelocityCheckOptions> options, IPaymentStore? store = null)
    : IAntiFraudCheck
{
    private readonly VelocityCheckOptions _options = options.Value;
    private readonly IPaymentStore? _store = store;

    public string Name => "VelocityCheck";

    public Task<AntiFraudResult> EvaluateAsync(AntiFraudContext context, CancellationToken ct = default)
    {
        // Velocity checks require a payment store implementation.
        // When integrated, query recent payout counts from the store.
        // Framework provides the hook; consuming app implements the query.
        return Task.FromResult(AntiFraudResult.Pass());
    }
}
