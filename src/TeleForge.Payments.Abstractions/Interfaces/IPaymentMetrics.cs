namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Metrics abstraction for payment operations.
/// </summary>
public interface IPaymentMetrics
{
    void InvoiceCreated(string botId, InvoiceType type, PaymentCurrency currency);
    void CheckoutCompleted(string botId, string payloadPrefix, bool approved);
    void PaymentSucceeded(string botId, string payloadPrefix, int amount, PaymentCurrency currency);
    void PaymentFailed(string botId, string payloadPrefix, string reason);
    void RefundProcessed(string botId, RefundReason reason, int amount);
    void PayoutRequested(PayoutProvider provider, PaymentCurrency currency);
    void PayoutCompleted(PayoutProvider provider, PaymentCurrency currency, PayoutStatus status);
    void PayoutDuration(PayoutProvider provider, double milliseconds);
    void GiftSent(string botId, GiftType giftType);
    void PaidMediaSold(string botId, int starCount);
    void AntiFraudCheck(string checkName, bool passed);
    IDisposable MeasurePaymentProcessing(string botId, string payloadPrefix);
}

/// <summary>
/// No-op implementation of payment metrics for when metrics are disabled.
/// </summary>
public sealed class NullPaymentMetrics : IPaymentMetrics
{
    public static readonly NullPaymentMetrics Instance = new();

    private NullPaymentMetrics()
    {
    }

    public void InvoiceCreated(string botId, InvoiceType type, PaymentCurrency currency)
    {
    }

    public void CheckoutCompleted(string botId, string payloadPrefix, bool approved)
    {
    }

    public void PaymentSucceeded(string botId, string payloadPrefix, int amount, PaymentCurrency currency)
    {
    }

    public void PaymentFailed(string botId, string payloadPrefix, string reason)
    {
    }

    public void RefundProcessed(string botId, RefundReason reason, int amount)
    {
    }

    public void PayoutRequested(PayoutProvider provider, PaymentCurrency currency)
    {
    }

    public void PayoutCompleted(PayoutProvider provider, PaymentCurrency currency, PayoutStatus status)
    {
    }

    public void PayoutDuration(PayoutProvider provider, double milliseconds)
    {
    }

    public void GiftSent(string botId, GiftType giftType)
    {
    }

    public void PaidMediaSold(string botId, int starCount)
    {
    }

    public void AntiFraudCheck(string checkName, bool passed)
    {
    }

    public IDisposable MeasurePaymentProcessing(string botId, string payloadPrefix)
    {
        return NullScope.Instance;
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
