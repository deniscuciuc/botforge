namespace TeleForge.Payments.Abstractions;

public class PayoutRequest
{
    public required long UserId { get; init; }
    public required int Amount { get; init; }
    public required PaymentCurrency Currency { get; init; }
    public required string Destination { get; init; }
    public PayoutProvider Provider { get; init; } = PayoutProvider.Fragment;
    public string? Network { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}
