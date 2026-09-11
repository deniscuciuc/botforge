namespace TeleForge.Payments.Abstractions;

public enum RefundReason
{
    UserRequested,
    PaymentFailed,
    Admin,
    TelegramInitiated
}
