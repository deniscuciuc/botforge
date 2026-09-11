namespace TeleForge.Payments.Abstractions;

public class InvoiceDefinition
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Payload { get; init; }
    public InvoiceType Type { get; init; } = InvoiceType.Product;
    public PaymentCurrency Currency { get; init; } = PaymentCurrency.Xtr;
    public required IReadOnlyList<LabeledAmount> Prices { get; init; }

    // Optional fields
    public string? PhotoUrl { get; init; }
    public int? PhotoWidth { get; init; }
    public int? PhotoHeight { get; init; }
    public int? MaxTipAmount { get; init; }
    public IReadOnlyList<int>? SuggestedTipAmounts { get; init; }
    public bool IsFlexible { get; init; }
    public bool NeedName { get; init; }
    public bool NeedPhoneNumber { get; init; }
    public bool NeedEmail { get; init; }
    public bool NeedShippingAddress { get; init; }
    public bool SendPhoneNumberToProvider { get; init; }
    public bool SendEmailToProvider { get; init; }

    /// <summary>
    /// The provider token. Empty string for Telegram Stars payments.
    /// </summary>
    public string ProviderToken { get; init; } = string.Empty;

    /// <summary>
    /// Provider-specific JSON data.
    /// </summary>
    public string? ProviderData { get; init; }

    /// <summary>
    /// Subscription period in seconds (for subscription invoices).
    /// </summary>
    public int? SubscriptionPeriod { get; init; }

    /// <summary>
    /// Business connection identifier.
    /// </summary>
    public string? BusinessConnectionId { get; init; }
}
