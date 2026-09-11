using Telegram.Bot.Types.Payments;

namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Wraps a Telegram RefundedPayment update with resolved metadata.
/// </summary>
public class RefundContext
{
    public required RefundedPayment Refund { get; init; }
    public required string BotId { get; init; }
    public required string PayloadPrefix { get; init; }
    public required string RawPayload { get; init; }
    public required long ChatId { get; init; }
    public required long UserId { get; init; }

    public string ChargeId => Refund.TelegramPaymentChargeId;
    public long TotalAmount => Refund.TotalAmount;
    public string Currency => Refund.Currency;

    /// <summary>
    /// Deserialize the raw payload into a typed payload.
    /// </summary>
    public T GetPayload<T>(Func<string[], T> deserializer)
    {
        ArgumentNullException.ThrowIfNull(deserializer);

        var parts = RawPayload.Split(':');
        var fields = parts.Length > 1 ? parts[1..] : [];
        return deserializer(fields);
    }

    /// <summary>
    /// Arbitrary data bag for passing data between refund processors.
    /// </summary>
    public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();
}
