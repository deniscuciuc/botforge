namespace TeleForge.Payments.Abstractions;

public class StarTransaction
{
    public required string Id { get; init; }
    public required int Amount { get; init; }
    public required TransactionDirection Direction { get; init; }
    public required DateTimeOffset Date { get; init; }
    public TransactionPartnerInfo? Partner { get; init; }
    public int? NanostarAmount { get; init; }
}

public class TransactionPartnerInfo
{
    public required string Type { get; init; }

    // User partner
    public long? UserId { get; init; }
    public string? Username { get; init; }
    public string? FirstName { get; init; }

    // Chat partner
    public long? ChatId { get; init; }
    public string? ChatTitle { get; init; }

    // Fragment partner
    public string? FragmentState { get; init; }

    // Affiliate
    public int? CommissionPerMille { get; init; }

    // Invoice payload (for user transactions)
    public string? InvoicePayload { get; init; }
}
