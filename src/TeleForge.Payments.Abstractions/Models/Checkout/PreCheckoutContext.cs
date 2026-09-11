using Telegram.Bot.Types.Payments;

namespace TeleForge.Payments.Abstractions;

/// <summary>
/// Wraps a Telegram PreCheckoutQuery with resolved metadata from the typed payload.
/// </summary>
public class PreCheckoutContext
{
    public required PreCheckoutQuery Query { get; init; }
    public required string BotId { get; init; }
    public required string PayloadPrefix { get; init; }
    public required string RawPayload { get; init; }
    public long UserId => Query.From.Id;
    public long TotalAmount => Query.TotalAmount;
    public string Currency => Query.Currency;
    public string PreCheckoutQueryId => Query.Id;

    /// <summary>
    /// Deserialize the raw payload into a typed payload.
    /// </summary>
    public T GetPayload<T>(Func<string[], T> deserializer)
    {
        ArgumentNullException.ThrowIfNull(deserializer);

        var parts = RawPayload.Split(':');
        // Skip prefix (first element)
        var fields = parts.Length > 1 ? parts[1..] : [];
        return deserializer(fields);
    }

    /// <summary>
    /// Arbitrary data bag for passing data between validators.
    /// </summary>
    public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();
}
