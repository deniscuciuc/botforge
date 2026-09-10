using System.Reflection;
using BotForge.Core;
using BotForge.Core.Enums;
using BotForge.Routing.Abstractions;
using BotForge.Routing.Authorization;

namespace BotForge.Routing;

public class TelegramRoutingOptions
{
    public HandlerDiscoveryMode DiscoveryMode { get; set; } = HandlerDiscoveryMode.Reflection;
    public List<Assembly> HandlerAssemblies { get; } = [];
    public CallbackAnswerStrategy DefaultCallbackAnswerStrategy { get; set; } = CallbackAnswerStrategy.AnswerFirst;

    internal List<Type> GlobalMiddleware { get; } = [];
    internal List<Type> CommandMiddleware { get; } = [];
    internal List<Type> CallbackMiddleware { get; } = [];
    internal List<Type> TextMiddleware { get; } = [];
    internal Dictionary<string, Action<PolicyBuilder>> PolicyConfigurations { get; } = [];

    public TelegramRoutingOptions AddHandlersFromAssembly(Assembly assembly)
    {
        HandlerAssemblies.Add(assembly);
        return this;
    }

    public TelegramRoutingOptions UseMiddleware<T>() where T : ITelegramMiddleware
    {
        GlobalMiddleware.Add(typeof(T));
        return this;
    }

    public TelegramRoutingOptions ConfigureCommands(Action<MiddlewareConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var config = new MiddlewareConfiguration(CommandMiddleware);
        configure(config);
        return this;
    }

    public TelegramRoutingOptions ConfigureCallbackQueries(Action<MiddlewareConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var config = new MiddlewareConfiguration(CallbackMiddleware);
        configure(config);
        return this;
    }

    public TelegramRoutingOptions ConfigureTextMessages(Action<MiddlewareConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var config = new MiddlewareConfiguration(TextMiddleware);
        configure(config);
        return this;
    }

    public TelegramRoutingOptions ConfigureAuthorization(Action<AuthorizationConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var config = new AuthorizationConfiguration(PolicyConfigurations);
        configure(config);
        return this;
    }
}

public class MiddlewareConfiguration
{
    private readonly List<Type> _middlewareTypes;

    internal MiddlewareConfiguration(List<Type> middlewareTypes)
    {
        _middlewareTypes = middlewareTypes;
    }

    public MiddlewareConfiguration UseMiddleware<T>() where T : ITelegramMiddleware
    {
        _middlewareTypes.Add(typeof(T));
        return this;
    }
}

public class AuthorizationConfiguration
{
    private readonly Dictionary<string, Action<PolicyBuilder>> _policies;

    internal AuthorizationConfiguration(Dictionary<string, Action<PolicyBuilder>> policies)
    {
        _policies = policies;
    }

    public AuthorizationConfiguration AddPolicy(string name, Action<PolicyBuilder> configure)
    {
        _policies[name] = configure;
        return this;
    }
}
