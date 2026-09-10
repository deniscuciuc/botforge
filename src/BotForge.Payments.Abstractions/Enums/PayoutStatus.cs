namespace BotForge.Payments.Abstractions;

public enum PayoutStatus
{
    Pending,
    Processing,
    Sent,
    ManualReview,
    Failed,
    Completed,
    Rejected
}
