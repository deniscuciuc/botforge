using BotForge.Payments.Abstractions;

namespace BotForge.Payments.Invoice;

public class InvoiceBuilder : IInvoiceBuilder
{
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _payload = string.Empty;
    private InvoiceType _type = InvoiceType.Product;
    private readonly List<LabeledAmount> _prices = [];
    private string? _photoUrl;
    private int? _photoWidth;
    private int? _photoHeight;
    private int? _maxTipAmount;
    private IReadOnlyList<int>? _suggestedTips;
    private string _providerToken = string.Empty;
    private string? _providerData;
    private int? _subscriptionPeriod;
    private string? _businessConnectionId;
    private bool _needName;
    private bool _needPhone;
    private bool _needEmail;
    private bool _needShipping;

    /// <summary>
    /// Creates a new builder configured for Telegram Stars payments.
    /// </summary>
    public static InvoiceBuilder Stars()
    {
        return new InvoiceBuilder();
    }

    public IInvoiceBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public IInvoiceBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public IInvoiceBuilder WithPayload(TypedPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        _payload = payload.Serialize();
        return this;
    }

    public IInvoiceBuilder WithRawPayload(string payload)
    {
        _payload = payload;
        return this;
    }

    public IInvoiceBuilder AddPrice(string label, int amount)
    {
        _prices.Add(new LabeledAmount(label, amount));
        return this;
    }

    public IInvoiceBuilder WithPhoto(string url, int? width = null, int? height = null)
    {
        _photoUrl = url;
        _photoWidth = width;
        _photoHeight = height;
        return this;
    }

    public IInvoiceBuilder WithMaxTipAmount(int maxTip)
    {
        _maxTipAmount = maxTip;
        return this;
    }

    public IInvoiceBuilder WithSuggestedTips(params int[] tips)
    {
        _suggestedTips = tips;
        return this;
    }

    public IInvoiceBuilder WithProviderToken(string token)
    {
        _providerToken = token;
        return this;
    }

    public IInvoiceBuilder WithProviderData(string data)
    {
        _providerData = data;
        return this;
    }

    public IInvoiceBuilder WithSubscriptionPeriod(int seconds)
    {
        _subscriptionPeriod = seconds;
        _type = InvoiceType.Subscription;
        return this;
    }

    public IInvoiceBuilder WithBusinessConnection(string connectionId)
    {
        _businessConnectionId = connectionId;
        return this;
    }

    public IInvoiceBuilder AsType(InvoiceType type)
    {
        _type = type;
        return this;
    }

    public IInvoiceBuilder RequireName()
    {
        _needName = true;
        return this;
    }

    public IInvoiceBuilder RequirePhoneNumber()
    {
        _needPhone = true;
        return this;
    }

    public IInvoiceBuilder RequireEmail()
    {
        _needEmail = true;
        return this;
    }

    public IInvoiceBuilder RequireShippingAddress()
    {
        _needShipping = true;
        return this;
    }

    public InvoiceDefinition Build()
    {
        if (string.IsNullOrWhiteSpace(_title))
            throw new InvalidOperationException("Invoice title is required.");
        if (string.IsNullOrWhiteSpace(_description))
            throw new InvalidOperationException("Invoice description is required.");
        if (string.IsNullOrWhiteSpace(_payload))
            throw new InvalidOperationException("Invoice payload is required.");
        if (_prices.Count == 0)
            throw new InvalidOperationException("At least one price is required.");

        return new InvoiceDefinition
        {
            Title = _title,
            Description = _description,
            Payload = _payload,
            Type = _type,
            Currency = PaymentCurrency.Xtr,
            Prices = _prices.AsReadOnly(),
            PhotoUrl = _photoUrl,
            PhotoWidth = _photoWidth,
            PhotoHeight = _photoHeight,
            MaxTipAmount = _maxTipAmount,
            SuggestedTipAmounts = _suggestedTips,
            ProviderToken = _providerToken,
            ProviderData = _providerData,
            SubscriptionPeriod = _subscriptionPeriod,
            BusinessConnectionId = _businessConnectionId,
            NeedName = _needName,
            NeedPhoneNumber = _needPhone,
            NeedEmail = _needEmail,
            NeedShippingAddress = _needShipping
        };
    }
}
