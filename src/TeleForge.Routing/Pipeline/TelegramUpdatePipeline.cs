using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TeleForge.Core;
using TeleForge.Routing.Registration;
using TeleForge.Routing.Routing;

namespace TeleForge.Routing.Pipeline;

public class TelegramUpdatePipeline : ITelegramUpdatePipeline
{
    private readonly TelegramUpdateDelegate _pipeline;

    public TelegramUpdatePipeline(
        TelegramRoutingOptions options,
        IHandlerRegistry registry,
        IServiceProvider rootServices,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);

        var commandRouter = new CommandRouter(registry, loggerFactory.CreateLogger<CommandRouter>());
        var clientProvider = rootServices.GetRequiredService<ITelegramBotClientProvider>();
        var callbackRouter = new CallbackQueryRouter(registry, clientProvider, options.DefaultCallbackAnswerStrategy,
            loggerFactory.CreateLogger<CallbackQueryRouter>());
        var textRouter = new TextMessageRouter(registry, loggerFactory.CreateLogger<TextMessageRouter>());
        var updateRouter = new UpdateTypeRouter(registry, loggerFactory.CreateLogger<UpdateTypeRouter>());

        var commandPipelineBuilder = new MiddlewarePipelineBuilder();
        foreach (var mw in options.CommandMiddleware.Select(mwType =>
                     (ITelegramMiddleware)ActivatorUtilities.CreateInstance(rootServices, mwType)))
            commandPipelineBuilder.UseMiddleware(mw);

        var commandPipeline = commandPipelineBuilder.Build(ctx => commandRouter.RouteAsync(ctx));

        var callbackPipelineBuilder = new MiddlewarePipelineBuilder();
        foreach (var mw in options.CallbackMiddleware.Select(mwType =>
                     (ITelegramMiddleware)ActivatorUtilities.CreateInstance(rootServices, mwType)))
            callbackPipelineBuilder.UseMiddleware(mw);

        var callbackPipeline = callbackPipelineBuilder.Build(ctx => callbackRouter.RouteAsync(ctx));

        var textPipelineBuilder = new MiddlewarePipelineBuilder();
        foreach (var mw in options.TextMiddleware.Select(mwType =>
                     (ITelegramMiddleware)ActivatorUtilities.CreateInstance(rootServices, mwType)))
            textPipelineBuilder.UseMiddleware(mw);

        var textPipeline = textPipelineBuilder.Build(ctx => textRouter.RouteAsync(ctx));

        var globalBuilder = new MiddlewarePipelineBuilder();
        foreach (var mw in options.GlobalMiddleware.Select(mwType =>
                     (ITelegramMiddleware)ActivatorUtilities.CreateInstance(rootServices, mwType)))
            globalBuilder.UseMiddleware(mw);

        _pipeline = globalBuilder.Build(UpdateDelegate);
        return;

        Task UpdateDelegate(TelegramUpdateContext ctx)
        {
            return ctx.UpdateType switch
            {
                "Message" when IsCommand(ctx) => commandPipeline(ctx),
                "Message" when HasDice(ctx) => updateRouter.RouteDiceAsync(ctx),
                "Message" when HasLocation(ctx) => updateRouter.RouteLocationAsync(ctx),
                "Message" when HasContact(ctx) => updateRouter.RouteContactAsync(ctx),
                "Message" when HasUsersShared(ctx) => updateRouter.RouteUsersSharedAsync(ctx),
                "Message" when HasChatShared(ctx) => updateRouter.RouteChatSharedAsync(ctx),
                "Message" when HasGift(ctx) => updateRouter.RouteGiftMessageAsync(ctx),
                "Message" when HasUniqueGift(ctx) => updateRouter.RouteUniqueGiftMessageAsync(ctx),
                "Message" when HasMedia(ctx) => updateRouter.RouteMediaAsync(ctx),
                "Message" when HasText(ctx) => textPipeline(ctx),
                "CallbackQuery" => callbackPipeline(ctx),
                "InlineQuery" => updateRouter.RouteInlineQueryAsync(ctx),
                "ChosenInlineResult" => updateRouter.RouteChosenInlineResultAsync(ctx),
                "PollAnswer" => updateRouter.RoutePollAnswerAsync(ctx),
                "MyChatMember" => updateRouter.RouteChatMemberAsync(ctx, true),
                "ChatMember" => updateRouter.RouteChatMemberAsync(ctx, false),
                _ => Task.CompletedTask
            };
        }
    }

    public Task ProcessAsync(TelegramUpdateContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.CancellationToken = ct;
        return _pipeline(context);
    }

    private static bool IsCommand(TelegramUpdateContext context)
    {
        var text = context.RawUpdate.Message?.Text;
        return text != null && text.StartsWith('/');
    }

    private static bool HasText(TelegramUpdateContext context)
    {
        return !string.IsNullOrEmpty(context.RawUpdate.Message?.Text);
    }

    private static bool HasDice(TelegramUpdateContext context)
    {
        return context.RawUpdate.Message?.Dice != null;
    }

    private static bool HasLocation(TelegramUpdateContext context)
    {
        return context.RawUpdate.Message?.Location != null;
    }

    private static bool HasContact(TelegramUpdateContext context)
    {
        return context.RawUpdate.Message?.Contact != null;
    }

    private static bool HasUsersShared(TelegramUpdateContext context)
    {
        return context.RawUpdate.Message?.UsersShared != null;
    }

    private static bool HasChatShared(TelegramUpdateContext context)
    {
        return context.RawUpdate.Message?.ChatShared != null;
    }

    private static bool HasMedia(TelegramUpdateContext context)
    {
        var msg = context.RawUpdate.Message;
        return msg != null && (msg.Photo is { Length: > 0 } || msg.Video != null || msg.Audio != null
                               || msg.Document != null || msg.Voice != null || msg.VideoNote != null
                               || msg.Sticker != null || msg.Animation != null);
    }

    private static bool HasGift(TelegramUpdateContext context)
    {
        return context.RawUpdate.Message?.Gift != null;
    }

    private static bool HasUniqueGift(TelegramUpdateContext context)
    {
        return context.RawUpdate.Message?.UniqueGift != null;
    }
}
