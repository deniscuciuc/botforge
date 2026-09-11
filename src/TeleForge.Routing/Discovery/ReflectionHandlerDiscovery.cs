using System.Reflection;
using Microsoft.Extensions.Logging;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Handlers;
using TeleForge.Routing.Registration;

namespace TeleForge.Routing.Discovery;

public class ReflectionHandlerDiscovery(ILogger<ReflectionHandlerDiscovery> logger)
{
    public void DiscoverAndRegister(IHandlerRegistry registry, IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(registry);

        foreach (var assembly in assemblies) DiscoverAssembly(registry, assembly);
    }

    private void DiscoverAssembly(IHandlerRegistry registry, Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).ToArray()!;
            logger.LogWarning(ex, "Could not load all types from assembly {Assembly}", assembly.FullName);
        }

        foreach (var type in types)
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            var commandAttr = type.GetCustomAttribute<TelegramCommandAttribute>();
            if (commandAttr != null && typeof(ICommandHandler).IsAssignableFrom(type))
            {
                RegisterCommand(registry, type, commandAttr);
                continue;
            }

            var callbackAttr = type.GetCustomAttribute<CallbackQueryAttribute>();
            if (callbackAttr != null && typeof(ICallbackQueryHandler).IsAssignableFrom(type))
            {
                RegisterCallback(registry, type, callbackAttr);
                continue;
            }

            var textAttr = type.GetCustomAttribute<TextMessageAttribute>();
            if (textAttr != null && typeof(ITextMessageHandler).IsAssignableFrom(type))
            {
                RegisterTextHandler(registry, type, textAttr);
                continue;
            }

            var inlineAttr = type.GetCustomAttribute<InlineQueryAttribute>();
            if (inlineAttr != null && typeof(IInlineQueryHandler).IsAssignableFrom(type))
            {
                registry.RegisterInlineQuery(new InlineQueryRegistration
                { HandlerType = type, Pattern = inlineAttr.Pattern });
                logger.LogDebug("Registered inline query handler: {Handler}", type.Name);
                continue;
            }

            if (type.GetCustomAttribute<ChosenInlineResultAttribute>() != null &&
                typeof(IChosenInlineResultHandler).IsAssignableFrom(type))
            {
                registry.RegisterChosenInlineResult(new ChosenInlineResultRegistration { HandlerType = type });
                logger.LogDebug("Registered chosen inline result handler: {Handler}", type.Name);
                continue;
            }

            var mediaAttr = type.GetCustomAttribute<MediaMessageAttribute>();
            if (mediaAttr != null && typeof(IMediaHandler).IsAssignableFrom(type))
            {
                registry.RegisterMedia(new MediaRegistration { HandlerType = type, MediaType = mediaAttr.MediaType });
                logger.LogDebug("Registered media handler: {MediaType} → {Handler}", mediaAttr.MediaType, type.Name);
                continue;
            }

            if (type.GetCustomAttribute<LocationMessageAttribute>() != null &&
                typeof(ILocationHandler).IsAssignableFrom(type))
            {
                registry.RegisterLocation(new LocationRegistration { HandlerType = type });
                logger.LogDebug("Registered location handler: {Handler}", type.Name);
                continue;
            }

            if (type.GetCustomAttribute<ContactMessageAttribute>() != null &&
                typeof(IContactHandler).IsAssignableFrom(type))
            {
                registry.RegisterContact(new ContactRegistration { HandlerType = type });
                logger.LogDebug("Registered contact handler: {Handler}", type.Name);
                continue;
            }

            if (type.GetCustomAttribute<UsersSharedMessageAttribute>() != null &&
                typeof(IUsersSharedHandler).IsAssignableFrom(type))
            {
                registry.RegisterUsersShared(new UsersSharedRegistration { HandlerType = type });
                logger.LogDebug("Registered users shared handler: {Handler}", type.Name);
                continue;
            }

            if (type.GetCustomAttribute<ChatSharedMessageAttribute>() != null &&
                typeof(IChatSharedHandler).IsAssignableFrom(type))
            {
                registry.RegisterChatShared(new ChatSharedRegistration { HandlerType = type });
                logger.LogDebug("Registered chat shared handler: {Handler}", type.Name);
                continue;
            }

            if (type.GetCustomAttribute<PollAnswerAttribute>() != null &&
                typeof(IPollAnswerHandler).IsAssignableFrom(type))
            {
                registry.RegisterPollAnswer(new PollAnswerRegistration { HandlerType = type });
                logger.LogDebug("Registered poll answer handler: {Handler}", type.Name);
                continue;
            }

            var chatMemberAttr = type.GetCustomAttribute<ChatMemberAttribute>();
            if (chatMemberAttr != null && typeof(IChatMemberHandler).IsAssignableFrom(type))
            {
                registry.RegisterChatMember(new ChatMemberRegistration
                { HandlerType = type, MyChatMemberOnly = chatMemberAttr.MyChatMemberOnly });
                logger.LogDebug("Registered chat member handler: {Handler}", type.Name);
                continue;
            }

            var diceAttr = type.GetCustomAttribute<DiceMessageAttribute>();
            if (diceAttr != null && typeof(IDiceHandler).IsAssignableFrom(type))
            {
                registry.RegisterDice(new DiceRegistration { HandlerType = type, Emoji = diceAttr.Emoji });
                logger.LogDebug("Registered dice handler: {Emoji} → {Handler}", diceAttr.Emoji ?? "(any)", type.Name);
                continue;
            }

            if (type.GetCustomAttribute<GiftMessageAttribute>() != null &&
                typeof(IGiftMessageHandler).IsAssignableFrom(type))
            {
                registry.RegisterGiftMessage(new GiftMessageRegistration { HandlerType = type });
                logger.LogDebug("Registered gift message handler: {Handler}", type.Name);
                continue;
            }

            if (type.GetCustomAttribute<UniqueGiftMessageAttribute>() != null &&
                typeof(IUniqueGiftMessageHandler).IsAssignableFrom(type))
            {
                registry.RegisterUniqueGiftMessage(new UniqueGiftMessageRegistration { HandlerType = type });
                logger.LogDebug("Registered unique gift message handler: {Handler}", type.Name);
            }
        }
    }

    private void RegisterCommand(IHandlerRegistry registry, Type type, TelegramCommandAttribute commandAttr)
    {
        var registration = new CommandRegistration
        {
            Command = commandAttr.Command,
            BotId = commandAttr.BotId,
            Description = commandAttr.Description,
            HandlerType = type,
            CommandAttribute = commandAttr,
            CommandScopeAttributes = type.GetCustomAttributes<TelegramCommandScopeAttribute>().ToArray(),
            AuthorizeAttributes = type.GetCustomAttributes<AuthorizeAttribute>().ToArray(),
            RateLimitAttribute = type.GetCustomAttribute<RateLimitAttribute>(),
            ChatTypeAttribute = type.GetCustomAttribute<ChatTypeAttribute>()
        };

        registry.RegisterCommand(registration);
        logger.LogDebug("Registered command handler: {Command} ({BotId}) → {Handler}", commandAttr.Command,
            commandAttr.BotId ?? "global", type.Name);
    }

    private void RegisterCallback(IHandlerRegistry registry, Type type, CallbackQueryAttribute callbackAttr)
    {
        var registration = new CallbackRegistration
        {
            Pattern = callbackAttr.Pattern,
            HandlerType = type,
            CallbackAttribute = callbackAttr,
            AuthorizeAttributes = type.GetCustomAttributes<AuthorizeAttribute>().ToArray(),
            RateLimitAttribute = type.GetCustomAttribute<RateLimitAttribute>(),
            IsRegex = callbackAttr.IsRegex
        };

        registry.RegisterCallback(registration);
        logger.LogDebug("Registered callback handler: {Pattern} → {Handler}", callbackAttr.Pattern, type.Name);
    }

    private void RegisterTextHandler(IHandlerRegistry registry, Type type, TextMessageAttribute textAttr)
    {
        var registration = new TextMessageRegistration
        {
            HandlerType = type,
            TextMessageAttribute = textAttr,
            Pattern = textAttr.Pattern,
            Priority = textAttr.Priority,
            AuthorizeAttributes = type.GetCustomAttributes<AuthorizeAttribute>().ToArray(),
            RateLimitAttribute = type.GetCustomAttribute<RateLimitAttribute>()
        };

        registry.RegisterTextHandler(registration);
        logger.LogDebug("Registered text handler: {Pattern} → {Handler}", textAttr.Pattern ?? "(catch-all)",
            type.Name);
    }
}
