namespace BotForge.Payments;

/// <summary>
/// Configuration options for the payments library.
/// </summary>
public class PaymentOptions
{
    /// <summary>
    /// Whether to automatically refund the payment when processing fails.
    /// Default: true (existing Service.Bot behavior).
    /// </summary>
    public bool AutoRefundOnFailure { get; set; } = true;

    // Payout worker
    public TimeSpan PayoutWorkerInterval { get; set; } = TimeSpan.FromSeconds(30);
    public int PayoutBatchSize { get; set; } = 10;
    public int PayoutMaxRetries { get; set; } = 3;

    // Revenue sync
    public bool RevenueSyncEnabled { get; set; }
    public TimeSpan RevenueSyncInterval { get; set; } = TimeSpan.FromMinutes(5);
}
