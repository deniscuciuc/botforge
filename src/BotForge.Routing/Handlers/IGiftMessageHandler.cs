using BotForge.Core;
using Telegram.Bot.Types;

namespace BotForge.Routing.Handlers;

/// <summary>
/// Handler for regular gift service messages (Message.Gift set).
/// The bot receives these when a user sends a Telegram Gift to a chat the bot manages,
/// or via a Business account connection.
/// </summary>
public interface IGiftMessageHandler
{
    Task HandleAsync(GiftMessageContext context, CancellationToken ct);
}

/// <summary>
/// Handler for unique (NFT) gift service messages (Message.UniqueGift set).
/// </summary>
public interface IUniqueGiftMessageHandler
{
    Task HandleAsync(UniqueGiftMessageContext context, CancellationToken ct);
}

public class GiftMessageContext(TelegramUpdateContext updateContext, Message message, GiftInfo giftInfo)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public Message Message { get; } = message;
    public GiftInfo GiftInfo { get; } = giftInfo;

    /// <summary>Identifier of the gift in the user's or Business account inventory (if available).</summary>
    public string? OwnedGiftId => GiftInfo.OwnedGiftId;

    /// <summary>Stars needed to convert the gift to Stars.</summary>
    public int? ConvertStarCount => GiftInfo.ConvertStarCount.HasValue ? (int)GiftInfo.ConvertStarCount.Value : null;

    /// <summary>Whether this regular gift can be upgraded to a unique gift.</summary>
    public bool CanBeUpgraded => GiftInfo.CanBeUpgraded;

    public long? ChatId => UpdateContext.ChatId;
    public long? UserId => UpdateContext.UserId;
    public string BotId => UpdateContext.BotId;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;
}

public class UniqueGiftMessageContext(
    TelegramUpdateContext updateContext,
    Message message,
    UniqueGiftInfo uniqueGiftInfo)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public Message Message { get; } = message;
    public UniqueGiftInfo UniqueGiftInfo { get; } = uniqueGiftInfo;

    /// <summary>Identifier of the unique gift in the inventory (if available).</summary>
    public string? OwnedGiftId => UniqueGiftInfo.OwnedGiftId;

    public long? ChatId => UpdateContext.ChatId;
    public long? UserId => UpdateContext.UserId;
    public string BotId => UpdateContext.BotId;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;
}
