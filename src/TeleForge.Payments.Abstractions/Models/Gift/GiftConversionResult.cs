namespace TeleForge.Payments.Abstractions;

public class GiftConversionResult
{
    public bool Success { get; init; }
    public int StarsReceived { get; init; }
    public string? Error { get; init; }

    public static GiftConversionResult Ok(int stars)
    {
        return new GiftConversionResult { Success = true, StarsReceived = stars };
    }

    public static GiftConversionResult Failed(string error)
    {
        return new GiftConversionResult { Success = false, Error = error };
    }
}
