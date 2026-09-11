namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Individual anti-fraud check executed during the payout validation pipeline.
/// </summary>
public interface IAntiFraudCheck
{
    string Name { get; }
    Task<AntiFraudResult> EvaluateAsync(AntiFraudContext context, CancellationToken ct = default);
}

public class AntiFraudContext
{
    public required long UserId { get; init; }
    public required int Amount { get; init; }
    public required PaymentCurrency Currency { get; init; }
    public required PayoutProvider Provider { get; init; }
    public required string Destination { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public IServiceProvider Services { get; init; } = null!;
}

public class AntiFraudResult
{
    public bool Passed { get; init; }
    public string? FailureReason { get; init; }
    public string? TemplateKey { get; init; }

    public static AntiFraudResult Pass()
    {
        return new AntiFraudResult { Passed = true };
    }

    public static AntiFraudResult Fail(string reason, string? templateKey = null)
    {
        return new AntiFraudResult { Passed = false, FailureReason = reason, TemplateKey = templateKey };
    }
}
