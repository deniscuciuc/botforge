using Microsoft.Extensions.Logging;
using TeleForge.Core;
using TeleForge.Payments.Abstractions;
using Telegram.Bot;

namespace TeleForge.Payments.Subscription;

public class SubscriptionService(
    ITelegramBotClientProvider botProvider,
    ILogger<SubscriptionService> logger)
    : ISubscriptionService
{
    public async Task<SubscriptionInviteLink> CreateSubscriptionLinkAsync(SubscriptionPlan plan,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var client = botProvider.GetClient(plan.BotId);
        var link = await client.CreateChatSubscriptionInviteLink(
            plan.ChatId,
            plan.SubscriptionPeriod,
            plan.SubscriptionPrice,
            plan.Name,
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Subscription link created for chat {ChatId}, period {Period}s, price {Price} Stars",
            plan.ChatId, plan.SubscriptionPeriod, plan.SubscriptionPrice);

        return new SubscriptionInviteLink
        {
            InviteLink = link.InviteLink,
            Name = link.Name,
            SubscriptionPeriod = link.SubscriptionPeriod ?? 0,
            SubscriptionPrice = link.SubscriptionPrice ?? 0,
            MemberLimit = link.MemberLimit,
            PendingJoinRequestCount = link.PendingJoinRequestCount
        };
    }

    public async Task<SubscriptionInviteLink> EditSubscriptionLinkAsync(string botId, string inviteLink,
        string? name = null, CancellationToken ct = default)
    {
        var client = botProvider.GetClient(botId);
        var link = await client.EditChatSubscriptionInviteLink(
            0, // The invite link identifies the chat
            inviteLink,
            name,
            ct).ConfigureAwait(false);

        logger.LogInformation("Subscription link edited: {InviteLink}", inviteLink);

        return new SubscriptionInviteLink
        {
            InviteLink = link.InviteLink,
            Name = link.Name,
            SubscriptionPeriod = link.SubscriptionPeriod ?? 0,
            SubscriptionPrice = link.SubscriptionPrice ?? 0,
            MemberLimit = link.MemberLimit,
            PendingJoinRequestCount = link.PendingJoinRequestCount
        };
    }

    public async Task EditUserSubscriptionAsync(string botId, long userId, string telegramChargeId, bool isCanceled,
        CancellationToken ct = default)
    {
        var client = botProvider.GetClient(botId);
        await client.EditUserStarSubscription(
            userId,
            telegramChargeId,
            isCanceled,
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "User Star subscription edited: user {UserId}, charge {ChargeId}, canceled {IsCanceled}",
            userId, telegramChargeId, isCanceled);
    }
}
