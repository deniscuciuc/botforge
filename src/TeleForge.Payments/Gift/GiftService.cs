using Microsoft.Extensions.Logging;
using TeleForge.Core;
using TeleForge.Payments.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleForge.Payments.Gift;

public class GiftService(
    ITelegramBotClientProvider botProvider,
    IPaymentMetrics metrics,
    ILogger<GiftService> logger)
    : IGiftService
{
    public async Task<IReadOnlyList<GiftDefinition>> GetAvailableGiftsAsync(string botId,
        CancellationToken ct = default)
    {
        var client = botProvider.GetClient(botId);
        var gifts = await client.GetAvailableGifts(ct).ConfigureAwait(false);

        var result = gifts.Gifts.Select(gift => new GiftDefinition
        {
            Id = gift.Id,
            StarCount = (int)gift.StarCount,
            TotalCount = gift.TotalCount,
            RemainingCount = gift.RemainingCount,
            StickerId = gift.Sticker.FileId
        })
            .ToList();

        logger.LogDebug("Retrieved {Count} available gifts", result.Count);
        return result;
    }

    public async Task SendGiftAsync(SendGiftRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var client = botProvider.GetClient(request.BotId);

        await client.SendGift(
            request.UserId,
            request.GiftId,
            request.Text,
            request.TextParseMode ?? default,
            payForUpgrade: request.PayForUpgrade,
            cancellationToken: ct).ConfigureAwait(false);

        metrics.GiftSent(request.BotId, GiftType.Regular);
        logger.LogInformation("Gift {GiftId} sent to user {UserId}", request.GiftId, request.UserId);
    }

    public async Task<OwnedGiftListResult> GetUserGiftsAsync(string botId, long userId, string? offset = null,
        int? limit = null, CancellationToken ct = default)
    {
        var client = botProvider.GetClient(botId);
        var result = await client.GetUserGifts(userId, offset: offset, limit: limit, cancellationToken: ct).ConfigureAwait(false);

        var gifts = result.Gifts.Select(MapOwnedGift).ToList();

        return new OwnedGiftListResult
        {
            Gifts = gifts,
            TotalCount = result.TotalCount,
            NextOffset = result.NextOffset
        };
    }

    public async Task<GiftConversionResult> ConvertToStarsAsync(string botId, long userId, string ownedGiftId,
        CancellationToken ct = default)
    {
        try
        {
            var client = botProvider.GetClient(botId);
            await client.ConvertGiftToStars(string.Empty, ownedGiftId, ct).ConfigureAwait(false);

            logger.LogInformation("Gift {GiftId} converted to Stars for user {UserId}", ownedGiftId, userId);

            // Telegram API does not return the star count from conversion, so we return 0 here
            return GiftConversionResult.Ok(0);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to convert gift {GiftId} to Stars for user {UserId}", ownedGiftId, userId);
            return GiftConversionResult.Failed(ex.Message);
        }
    }

    public async Task TransferGiftAsync(string botId, long senderUserId, string ownedGiftId, long targetUserId,
        int? starCount = null, CancellationToken ct = default)
    {
        var client = botProvider.GetClient(botId);
        await client.TransferGift(
            string.Empty,
            ownedGiftId,
            targetUserId,
            starCount,
            ct).ConfigureAwait(false);

        logger.LogInformation("Gift {GiftId} transferred from user {FromUserId} to {ToUserId}", ownedGiftId,
            senderUserId, targetUserId);
    }

    public async Task UpgradeToUniqueAsync(string botId, long userId, string ownedGiftId,
        CancellationToken ct = default)
    {
        var client = botProvider.GetClient(botId);
        await client.UpgradeGift(string.Empty, ownedGiftId, cancellationToken: ct).ConfigureAwait(false);

        metrics.GiftSent(botId, GiftType.Unique);
        logger.LogInformation("Gift {GiftId} upgraded to unique/NFT for user {UserId}", ownedGiftId, userId);
    }

    public async Task GiftPremiumAsync(string botId, long userId, int months, CancellationToken ct = default)
    {
        var client = botProvider.GetClient(botId);
        var starCount = months switch
        {
            3 => 1000L,
            6 => 1500L,
            12 => 2500L,
            _ => throw new ArgumentOutOfRangeException(nameof(months), months, "Must be 3, 6, or 12")
        };

        await client.GiftPremiumSubscription(
            userId,
            months,
            starCount,
            cancellationToken: ct).ConfigureAwait(false);

        logger.LogInformation("Gifted {Months}-month Premium to user {UserId}", months, userId);
    }

    public async Task<OwnedGiftListResult> GetBusinessAccountGiftsAsync(string botId, string businessConnectionId,
        string? offset = null, int? limit = null, CancellationToken ct = default)
    {
        var client = botProvider.GetClient(botId);
        var result = await client.GetBusinessAccountGifts(
            businessConnectionId,
            offset: offset,
            limit: limit,
            cancellationToken: ct).ConfigureAwait(false);

        return new OwnedGiftListResult
        {
            Gifts = result.Gifts.Select(MapOwnedGift).ToList(),
            TotalCount = result.TotalCount,
            NextOffset = result.NextOffset
        };
    }

    public async Task<GiftConversionResult> ConvertBusinessGiftToStarsAsync(string botId,
        string businessConnectionId, string ownedGiftId, CancellationToken ct = default)
    {
        try
        {
            var client = botProvider.GetClient(botId);
            await client.ConvertGiftToStars(businessConnectionId, ownedGiftId, ct).ConfigureAwait(false);

            logger.LogInformation(
                "Business gift {GiftId} converted to Stars via connection {BizConn}", ownedGiftId,
                businessConnectionId);

            return GiftConversionResult.Ok(0);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to convert business gift {GiftId} to Stars", ownedGiftId);
            return GiftConversionResult.Failed(ex.Message);
        }
    }

    private static OwnedGiftInfo MapOwnedGift(OwnedGift owned)
    {
        return owned switch
        {
            OwnedGiftRegular regular => new OwnedGiftRegularInfo
            {
                GiftId = regular.Gift.Id,
                Type = GiftType.Regular,
                SenderUserId = regular.SenderUser?.Id,
                Text = regular.Text,
                Date = regular.SendDate,
                IsSaved = regular.IsSaved,
                StarCount = (int)regular.Gift.StarCount,
                ConvertStarCount = regular.ConvertStarCount.HasValue ? (int)regular.ConvertStarCount.Value : null,
                PrepaidUpgradeStarCount = regular.PrepaidUpgradeStarCount.HasValue
                    ? (int)regular.PrepaidUpgradeStarCount.Value
                    : null,
                CanUpgrade = regular.CanBeUpgraded,
                IsPrivate = regular.IsPrivate
            },
            OwnedGiftUnique unique => new OwnedGiftUniqueInfo
            {
                GiftId = unique.Gift.GiftId,
                Type = GiftType.Unique,
                SenderUserId = unique.SenderUser?.Id,
                Text = null,
                Date = unique.SendDate,
                IsSaved = unique.IsSaved,
                Name = unique.Gift.Name,
                Number = unique.Gift.Number,
                Model = unique.Gift.Model?.Name,
                Backdrop = unique.Gift.Backdrop?.Name,
                Symbol = unique.Gift.Symbol?.Name,
                CanTransfer = unique.CanBeTransferred,
                TransferStarCount = unique.TransferStarCount.HasValue ? (int)unique.TransferStarCount.Value : null
            },
            _ => throw new InvalidOperationException($"Unknown owned gift type: {owned.GetType().Name}")
        };
    }
}
