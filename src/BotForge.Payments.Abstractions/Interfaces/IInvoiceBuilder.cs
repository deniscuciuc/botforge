namespace BotForge.Payments.Abstractions;

/// <summary>
/// Fluent builder for creating Telegram Star invoices.
/// </summary>
public interface IInvoiceBuilder
{
    IInvoiceBuilder WithTitle(string title);
    IInvoiceBuilder WithDescription(string description);
    IInvoiceBuilder WithPayload(TypedPayload payload);
    IInvoiceBuilder WithRawPayload(string payload);
    IInvoiceBuilder AddPrice(string label, int amount);
    IInvoiceBuilder WithPhoto(string url, int? width = null, int? height = null);
    IInvoiceBuilder WithMaxTipAmount(int maxTip);
    IInvoiceBuilder WithSuggestedTips(params int[] tips);
    IInvoiceBuilder WithProviderToken(string token);
    IInvoiceBuilder WithProviderData(string data);
    IInvoiceBuilder WithSubscriptionPeriod(int seconds);
    IInvoiceBuilder WithBusinessConnection(string connectionId);
    IInvoiceBuilder AsType(InvoiceType type);
    IInvoiceBuilder RequireName();
    IInvoiceBuilder RequirePhoneNumber();
    IInvoiceBuilder RequireEmail();
    IInvoiceBuilder RequireShippingAddress();
    InvoiceDefinition Build();
}
