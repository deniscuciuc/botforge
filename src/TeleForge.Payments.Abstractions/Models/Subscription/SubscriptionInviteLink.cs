namespace TeleForge.Payments.Abstractions;

public class SubscriptionInviteLink
{
    public required string InviteLink { get; init; }
    public string? Name { get; init; }
    public int SubscriptionPeriod { get; init; }
    public int SubscriptionPrice { get; init; }
    public int? MemberLimit { get; init; }
    public int? PendingJoinRequestCount { get; init; }
}
