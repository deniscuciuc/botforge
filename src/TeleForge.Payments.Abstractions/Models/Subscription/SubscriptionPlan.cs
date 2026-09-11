namespace TeleForge.Payments.Abstractions;

public class SubscriptionPlan
{
    public required long ChatId { get; init; }
    public required string BotId { get; init; }

    /// <summary>
    /// Subscription period in seconds. Standard: 2592000 (30 days).
    /// </summary>
    public required int SubscriptionPeriod { get; init; }

    /// <summary>
    /// Star price the user pays for each subscription period.
    /// </summary>
    public required int SubscriptionPrice { get; init; }

    public string? Name { get; init; }
    public int? MemberLimit { get; init; }
}
