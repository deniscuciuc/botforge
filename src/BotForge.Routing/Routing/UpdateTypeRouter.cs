using System.Diagnostics;
using BotForge.Core;
using BotForge.Routing.Handlers;
using BotForge.Routing.Metrics;
using BotForge.Routing.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BotForge.Routing.Routing;

public class UpdateTypeRouter(IHandlerRegistry registry, ILogger<UpdateTypeRouter> logger)
{
    public async Task RouteInlineQueryAsync(TelegramUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var inlineQuery = context.RawUpdate.InlineQuery;
        if (inlineQuery == null) return;

        var reg = registry.FindInlineQuery(inlineQuery.Query);
        if (reg == null)
        {
            logger.LogDebug("No inline query handler found for: {Query}",
                inlineQuery.Query[..Math.Min(inlineQuery.Query.Length, 50)]);
            return;
        }

        var handler = (IInlineQueryHandler)ActivatorUtilities.CreateInstance(context.RequestServices, reg.HandlerType);
        await TrackHandlerExecution(context, "inline_query", reg.HandlerType.Name, () =>
            handler.HandleAsync(new InlineQueryContext(context, inlineQuery), context.CancellationToken)).ConfigureAwait(false);
    }

    public async Task RouteChosenInlineResultAsync(TelegramUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var chosen = context.RawUpdate.ChosenInlineResult;
        if (chosen == null) return;

        var reg = registry.FindChosenInlineResult();
        if (reg == null) return;

        var handler =
            (IChosenInlineResultHandler)ActivatorUtilities.CreateInstance(context.RequestServices, reg.HandlerType);
        await TrackHandlerExecution(context, "chosen_inline_result", reg.HandlerType.Name, () =>
            handler.HandleAsync(new ChosenInlineResultContext(context, chosen), context.CancellationToken)).ConfigureAwait(false);
    }

    public async Task RouteMediaAsync(TelegramUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var msg = context.RawUpdate.Message;
        if (msg == null) return;

        var mediaType = DetectMediaType(msg);
        if (mediaType == null) return;

        var reg = registry.FindMedia(mediaType.Value);
        if (reg == null)
        {
            logger.LogDebug("No media handler found for type: {MediaType}", mediaType);
            return;
        }

        var handler = (IMediaHandler)ActivatorUtilities.CreateInstance(context.RequestServices, reg.HandlerType);
        await TrackHandlerExecution(context, "media", $"{mediaType}", () =>
            handler.HandleAsync(new MediaContext(context, msg, mediaType.Value), context.CancellationToken)).ConfigureAwait(false);
    }

    public async Task RouteLocationAsync(TelegramUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var location = context.RawUpdate.Message?.Location;
        if (location == null) return;

        var reg = registry.FindLocation();
        if (reg == null) return;

        var handler = (ILocationHandler)ActivatorUtilities.CreateInstance(context.RequestServices, reg.HandlerType);
        await TrackHandlerExecution(context, "location", reg.HandlerType.Name, () =>
            handler.HandleAsync(new LocationContext(context, location), context.CancellationToken)).ConfigureAwait(false);
    }

    public async Task RouteContactAsync(TelegramUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var contact = context.RawUpdate.Message?.Contact;
        if (contact == null) return;

        var reg = registry.FindContact();
        if (reg == null) return;

        var handler = (IContactHandler)ActivatorUtilities.CreateInstance(context.RequestServices, reg.HandlerType);
        await TrackHandlerExecution(context, "contact", reg.HandlerType.Name, () =>
            handler.HandleAsync(new ContactContext(context, contact), context.CancellationToken)).ConfigureAwait(false);
    }

    public async Task RouteUsersSharedAsync(TelegramUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var usersShared = context.RawUpdate.Message?.UsersShared;
        if (usersShared == null) return;

        var reg = registry.FindUsersShared();
        if (reg == null) return;

        var handler = (IUsersSharedHandler)ActivatorUtilities.CreateInstance(context.RequestServices, reg.HandlerType);
        await TrackHandlerExecution(context, "users_shared", reg.HandlerType.Name, () =>
            handler.HandleAsync(new UsersSharedContext(context, usersShared), context.CancellationToken)).ConfigureAwait(false);
    }

    public async Task RouteChatSharedAsync(TelegramUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var chatShared = context.RawUpdate.Message?.ChatShared;
        if (chatShared == null) return;

        var reg = registry.FindChatShared();
        if (reg == null) return;

        var handler = (IChatSharedHandler)ActivatorUtilities.CreateInstance(context.RequestServices, reg.HandlerType);
        await TrackHandlerExecution(context, "chat_shared", reg.HandlerType.Name, () =>
            handler.HandleAsync(new ChatSharedContext(context, chatShared), context.CancellationToken)).ConfigureAwait(false);
    }

    public async Task RoutePollAnswerAsync(TelegramUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var pollAnswer = context.RawUpdate.PollAnswer;
        if (pollAnswer == null) return;

        var reg = registry.FindPollAnswer();
        if (reg == null) return;

        var handler = (IPollAnswerHandler)ActivatorUtilities.CreateInstance(context.RequestServices, reg.HandlerType);
        await TrackHandlerExecution(context, "poll_answer", reg.HandlerType.Name, () =>
            handler.HandleAsync(new PollAnswerContext(context, pollAnswer), context.CancellationToken)).ConfigureAwait(false);
    }

    public async Task RouteChatMemberAsync(TelegramUpdateContext context, bool isMyChatMember)
    {
        ArgumentNullException.ThrowIfNull(context);

        var memberUpdate = isMyChatMember ? context.RawUpdate.MyChatMember : context.RawUpdate.ChatMember;
        if (memberUpdate == null) return;

        var reg = registry.FindChatMember(isMyChatMember);
        if (reg == null) return;

        var handler = (IChatMemberHandler)ActivatorUtilities.CreateInstance(context.RequestServices, reg.HandlerType);
        await TrackHandlerExecution(context, "chat_member", reg.HandlerType.Name, () =>
            handler.HandleAsync(new ChatMemberContext(context, memberUpdate, isMyChatMember),
                context.CancellationToken)).ConfigureAwait(false);
    }

    public async Task RouteDiceAsync(TelegramUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var dice = context.RawUpdate.Message?.Dice;
        if (dice == null) return;

        var reg = registry.FindDice(dice.Emoji);
        if (reg == null) return;

        var handler = (IDiceHandler)ActivatorUtilities.CreateInstance(context.RequestServices, reg.HandlerType);
        await TrackHandlerExecution(context, "dice", $"{dice.Emoji}", () =>
            handler.HandleAsync(new DiceContext(context, dice), context.CancellationToken)).ConfigureAwait(false);
    }

    public async Task RouteGiftMessageAsync(TelegramUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var msg = context.RawUpdate.Message;
        var giftInfo = msg?.Gift;
        if (msg == null || giftInfo == null) return;

        var reg = registry.FindGiftMessage();
        if (reg == null) return;

        var handler =
            (IGiftMessageHandler)ActivatorUtilities.CreateInstance(context.RequestServices, reg.HandlerType);
        await TrackHandlerExecution(context, "gift_message", reg.HandlerType.Name, () =>
            handler.HandleAsync(new GiftMessageContext(context, msg, giftInfo), context.CancellationToken)).ConfigureAwait(false);
    }

    public async Task RouteUniqueGiftMessageAsync(TelegramUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var msg = context.RawUpdate.Message;
        var uniqueGiftInfo = msg?.UniqueGift;
        if (msg == null || uniqueGiftInfo == null) return;

        var reg = registry.FindUniqueGiftMessage();
        if (reg == null) return;

        var handler =
            (IUniqueGiftMessageHandler)ActivatorUtilities.CreateInstance(context.RequestServices, reg.HandlerType);
        await TrackHandlerExecution(context, "unique_gift_message", reg.HandlerType.Name, () =>
            handler.HandleAsync(new UniqueGiftMessageContext(context, msg, uniqueGiftInfo),
                context.CancellationToken)).ConfigureAwait(false);
    }

    private static MediaType? DetectMediaType(global::Telegram.Bot.Types.Message msg)
    {
        if (msg.Photo is { Length: > 0 }) return MediaType.Photo;
        if (msg.Video != null) return MediaType.Video;
        if (msg.Audio != null) return MediaType.Audio;
        if (msg.Document != null) return MediaType.Document;
        if (msg.Voice != null) return MediaType.Voice;
        if (msg.VideoNote != null) return MediaType.VideoNote;
        if (msg.Sticker != null) return MediaType.Sticker;
        if (msg.Animation != null) return MediaType.Animation;
        return null;
    }

    private static async Task TrackHandlerExecution(
        TelegramUpdateContext context, string queryType, string query, Func<Task> handlerInvoke)
    {
        var sw = Stopwatch.StartNew();
        var hadException = false;
        try
        {
            await handlerInvoke().ConfigureAwait(false);
        }
        catch
        {
            hadException = true;
            throw;
        }
        finally
        {
            sw.Stop();
            var status = hadException ? "exception" : context.Result?.Success == false ? "blocked" : "success";
            RoutingMetrics.RecordHandlerCall(context.BotId, queryType, query, sw.Elapsed, status);
        }
    }
}
