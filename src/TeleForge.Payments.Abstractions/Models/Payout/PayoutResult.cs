namespace TeleForge.Payments.Abstractions;

public class PayoutResult
{
    public bool Success { get; init; }
    public PayoutStatus Status { get; init; }
    public string? TransactionId { get; init; }
    public PayoutProvider Provider { get; init; }
    public string? Error { get; init; }

    public static PayoutResult Ok(string transactionId, PayoutProvider provider)
    {
        return new PayoutResult
        { Success = true, Status = PayoutStatus.Completed, TransactionId = transactionId, Provider = provider };
    }

    public static PayoutResult Queued(PayoutProvider provider)
    {
        return new PayoutResult { Success = true, Status = PayoutStatus.Processing, Provider = provider };
    }

    public static PayoutResult NeedsReview(PayoutProvider provider)
    {
        return new PayoutResult { Success = true, Status = PayoutStatus.ManualReview, Provider = provider };
    }

    public static PayoutResult Failed(string error, PayoutProvider provider)
    {
        return new PayoutResult { Success = false, Status = PayoutStatus.Failed, Error = error, Provider = provider };
    }
}
