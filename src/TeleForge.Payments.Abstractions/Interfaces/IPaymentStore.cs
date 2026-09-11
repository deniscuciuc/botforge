namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Persistence abstraction for payment records. Implemented by the consuming application.
/// </summary>
public interface IPaymentStore
{
    // Payments
    Task RecordPaymentAsync(PaymentRecord payment, CancellationToken ct = default);
    Task<PaymentRecord?> GetByChargeIdAsync(string chargeId, CancellationToken ct = default);
    Task UpdatePaymentStatusAsync(string chargeId, PaymentStatus status, CancellationToken ct = default);

    // Refunds
    Task RecordRefundAsync(RefundRecord refund, CancellationToken ct = default);
    Task<bool> IsRefundRecordedAsync(string chargeId, CancellationToken ct = default);

    // Payouts
    Task<string> SavePayoutAsync(PayoutRecord payout, CancellationToken ct = default);
    Task<IReadOnlyList<PayoutRecord>> GetPendingPayoutsAsync(int limit, CancellationToken ct = default);

    Task UpdatePayoutStatusAsync(string payoutId, PayoutStatus status, string? transactionId, string? error,
        CancellationToken ct = default);

    // Star transactions
    Task SaveStarTransactionsAsync(IEnumerable<StarTransactionRecord> transactions, CancellationToken ct = default);
    Task<DateTimeOffset?> GetLastTransactionSyncDateAsync(string botId, CancellationToken ct = default);
}

public class PaymentRecord
{
    public required string ChargeId { get; init; }
    public string? ProviderChargeId { get; init; }
    public required long UserId { get; init; }
    public required int Amount { get; init; }
    public required string Currency { get; init; }
    public required string Payload { get; init; }
    public required PaymentStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public class RefundRecord
{
    public required string ChargeId { get; init; }
    public required long UserId { get; init; }
    public required int Amount { get; init; }
    public required string Currency { get; init; }
    public required string Payload { get; init; }
    public required RefundReason Reason { get; init; }
    public bool BalanceReversed { get; init; }
    public string? Description { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public class PayoutRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public required long UserId { get; init; }
    public required int Amount { get; init; }
    public required PaymentCurrency Currency { get; init; }
    public required string Destination { get; init; }
    public required PayoutProvider Provider { get; init; }
    public PayoutStatus Status { get; init; } = PayoutStatus.Pending;
    public string? TransactionId { get; init; }
    public string? Error { get; init; }
    public int RetryCount { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; init; }
}

public class StarTransactionRecord
{
    public required string TransactionId { get; init; }
    public required string BotId { get; init; }
    public required int Amount { get; init; }
    public required TransactionDirection Direction { get; init; }
    public required DateTimeOffset Date { get; init; }
    public string? PartnerType { get; init; }
    public long? PartnerUserId { get; init; }
    public string? InvoicePayload { get; init; }
}
