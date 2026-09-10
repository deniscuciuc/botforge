namespace BotForge.Payments.Abstractions;

/// <summary>
/// Manages channel subscription invite links and user Star subscriptions.
/// </summary>
public interface ISubscriptionService
{
    Task<SubscriptionInviteLink> CreateSubscriptionLinkAsync(SubscriptionPlan plan, CancellationToken ct = default);

    Task<SubscriptionInviteLink> EditSubscriptionLinkAsync(string botId, string inviteLink, string? name = null,
        CancellationToken ct = default);

    Task EditUserSubscriptionAsync(string botId, long userId, string telegramChargeId, bool isCanceled,
        CancellationToken ct = default);
}
