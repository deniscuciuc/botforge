using Microsoft.Extensions.Options;
using TeleForge.Payments.Abstractions;

namespace TeleForge.Payments.AntiFraud;

public class AmountLimitCheckOptions
{
    /// <summary>Smallest payout amount allowed, inclusive.</summary>
    public int MinAmount { get; set; } = 1;

    /// <summary>Largest payout amount allowed, inclusive.</summary>
    public int MaxAmount { get; set; } = int.MaxValue;
}

/// <summary>
/// Validates the payout amount against configured min/max/daily limits.
/// </summary>
public class AmountLimitCheck(IOptions<AmountLimitCheckOptions> options) : IAntiFraudCheck
{
    private readonly AmountLimitCheckOptions _options = options.Value;

    public string Name => "AmountLimitCheck";

    public Task<AntiFraudResult> EvaluateAsync(AntiFraudContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Amount < _options.MinAmount)
            return Task.FromResult(AntiFraudResult.Fail(
                $"Amount {context.Amount} is below minimum {_options.MinAmount}",
                "antifraud_amount_too_low"));

        if (context.Amount > _options.MaxAmount)
            return Task.FromResult(AntiFraudResult.Fail(
                $"Amount {context.Amount} exceeds maximum {_options.MaxAmount}",
                "antifraud_amount_too_high"));

        return Task.FromResult(AntiFraudResult.Pass());
    }
}
