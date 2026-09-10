namespace BotForge.Payments.Abstractions;

public enum RefundReason
{
    UserRequested,
    PaymentFailed,
    Admin,
    TelegramInitiated
}
