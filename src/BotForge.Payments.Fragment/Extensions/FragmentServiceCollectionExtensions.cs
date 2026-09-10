using BotForge.Payments.Abstractions;
using BotForge.Payments.Fragment.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace BotForge.Payments.Fragment.Extensions;

public static class FragmentServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Fragment payout provider and its dependencies.
    /// </summary>
    public static IServiceCollection AddFragmentPayoutProvider(
        this IServiceCollection services,
        Action<FragmentOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.TryAddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<FragmentOptions>>().Value;
            var sdk = new FragmentSdk(
                null,
                opts.Timeout,
                baseAddress: opts.BaseAddress);
            return sdk;
        });

        services.TryAddSingleton<FragmentAuthManager>();

        services.TryAddSingleton<FragmentClient>();

        services.TryAddSingleton<FragmentMetrics>();

        services.AddSingleton<IPayoutProvider, FragmentPayoutProvider>();

        services.AddSingleton<IStartupInitializer>(sp =>
            new FragmentStartupInitializer(sp.GetRequiredService<FragmentAuthManager>()));

        return services;
    }

    /// <summary>
    /// Adds Fragment health check to the health check builder.
    /// </summary>
    public static IServiceCollection AddFragmentHealthCheck(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<FragmentHealthCheck>("fragment-api", tags: ["fragment", "payout"]);
        return services;
    }
}

/// <summary>
/// Marker interface for startup initialization.
/// Consumers should resolve all <see cref="IStartupInitializer"/> and call <see cref="InitializeAsync"/>.
/// </summary>
public interface IStartupInitializer
{
    Task InitializeAsync(CancellationToken ct = default);
}

internal sealed class FragmentStartupInitializer(FragmentAuthManager authManager) : IStartupInitializer
{
    public Task InitializeAsync(CancellationToken ct = default)
    {
        authManager.SeedTokens();
        return Task.CompletedTask;
    }
}
