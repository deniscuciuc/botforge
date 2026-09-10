using BotForge.Core;
using BotForge.Routing.Abstractions;
using BotForge.Routing.Authorization;
using BotForge.Routing.CallbackData;
using BotForge.Routing.Discovery;
using BotForge.Routing.Pipeline;
using BotForge.Routing.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BotForge.Routing.Extensions;

public static class RoutingServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramRouting(
        this IServiceCollection services,
        Action<TelegramRoutingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new TelegramRoutingOptions();
        configure(options);

        var policies = new Dictionary<string, IAuthorizationPolicy>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, configurePolicy) in options.PolicyConfigurations)
        {
            var builder = new PolicyBuilder();
            configurePolicy(builder);
            policies[name] = builder.Build(name);
        }

        IReadOnlyDictionary<string, IAuthorizationPolicy> policyDict = policies;

        services.AddSingleton(options);
        services.AddSingleton(policyDict);

        services.AddSingleton<ReflectionHandlerDiscovery>();

        services.AddSingleton<IHandlerRegistry>(sp =>
        {
            var registry = new HandlerRegistry();

            if (options is { DiscoveryMode: HandlerDiscoveryMode.Reflection, HandlerAssemblies.Count: > 0 })
            {
                var discovery = sp.GetRequiredService<ReflectionHandlerDiscovery>();
                discovery.DiscoverAndRegister(registry, options.HandlerAssemblies.Distinct());
            }

            return registry;
        });

        services.TryAddSingleton<ICallbackDataSerializer, CallbackDataSerializer>();

        services.TryAddSingleton<IConversationStateStore, InMemoryConversationStateStore>();

        services.TryAddSingleton<INavigationService, InMemoryNavigationService>();

        services.TryAddSingleton<IUserLocaleResolver>(new TelegramLanguageCodeResolver());

        services.AddSingleton<ITelegramUpdatePipeline>(sp =>
        {
            var registry = sp.GetRequiredService<IHandlerRegistry>();

            return new TelegramUpdatePipeline(
                options,
                registry,
                sp,
                sp.GetRequiredService<ILoggerFactory>());
        });

        return services;
    }
}
