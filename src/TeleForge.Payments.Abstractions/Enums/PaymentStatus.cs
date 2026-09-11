namespace TeleForge.Payments.Abstractions;

public enum PaymentStatus
{
    Created,
    PendingCheckout,
    Paid,
    Failed,
    Refunded,
    PartialRefund
}
