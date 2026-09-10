namespace BotForge.Payments.Abstractions;

public class InvoiceResult
{
    public bool Success { get; init; }
    public string? InvoiceLink { get; init; }
    public int? MessageId { get; init; }
    public string? Error { get; init; }

    public static InvoiceResult Link(string url)
    {
        return new InvoiceResult { Success = true, InvoiceLink = url };
    }

    public static InvoiceResult Sent(int messageId)
    {
        return new InvoiceResult { Success = true, MessageId = messageId };
    }

    public static InvoiceResult Failed(string error)
    {
        return new InvoiceResult { Success = false, Error = error };
    }
}
