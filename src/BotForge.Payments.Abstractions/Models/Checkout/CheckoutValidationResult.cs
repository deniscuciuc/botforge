namespace BotForge.Payments.Abstractions;

public class CheckoutValidationResult
{
    public bool Approved { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorTemplateKey { get; init; }

    public static CheckoutValidationResult Approve()
    {
        return new CheckoutValidationResult { Approved = true };
    }

    public static CheckoutValidationResult Reject(string errorTemplateKey, string? message = null)
    {
        return new CheckoutValidationResult
        { Approved = false, ErrorTemplateKey = errorTemplateKey, ErrorMessage = message };
    }
}
