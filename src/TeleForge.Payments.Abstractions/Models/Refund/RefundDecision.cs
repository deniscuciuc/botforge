namespace TeleForge.Payments.Abstractions;

public class RefundDecision
{
    public bool ReverseBalance { get; init; }
    public int Amount { get; init; }
    public RefundReason Reason { get; init; }
    public string? Description { get; init; }

    public static RefundDecision Reverse(int amount, RefundReason reason, string? description = null)
    {
        return new RefundDecision
        { ReverseBalance = true, Amount = amount, Reason = reason, Description = description };
    }

    public static RefundDecision Skip(string description)
    {
        return new RefundDecision { ReverseBalance = false, Description = description };
    }
}
