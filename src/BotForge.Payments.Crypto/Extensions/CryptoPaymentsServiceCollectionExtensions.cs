using BotForge.Payments.Crypto.DirectAddress;
using BotForge.Payments.Crypto.Observer;
using BotForge.Payments.Crypto.Options;
using BotForge.Payments.Crypto.Rpc;
using BotForge.Payments.Crypto.TonConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BotForge.Payments.Crypto.Extensions;

/// <summary>
/// DI registration helpers for the TON/crypto payment rails.
/// Call <see cref="AddTonWalletPayments"/> to enable TON Connect + direct-address support.
/// </summary>
public static class CryptoPaymentsServiceCollectionExtensions
{
    /// <summary>
    /// Registers all TON wallet payment services: TON Connect session factory,
    /// direct-address session factory, default Toncenter RPC client, and the
    /// blockchain observer background worker.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Required action to set at minimum <see cref="TonPaymentOptions.DepositAddress"/>.</param>
    public static IServiceCollection AddTonWalletPayments(
        this IServiceCollection services,
        Action<TonPaymentOptions> configure)
    {
        services.Configure(configure);

        // Named HttpClient pointed at the configured RPC endpoint
        services.AddHttpClient(TonPaymentOptions.HttpClientName, (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<TonPaymentOptions>>().Value;
            client.BaseAddress = new Uri(opts.RpcEndpoint);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddSingleton<ITonRpcClient, ToncenterRpcClient>();
        services.AddSingleton<TonConnectSessionService>();
        services.AddSingleton<TonDirectAddressService>();
        services.AddHostedService<TonBlockchainObserverWorker>();

        return services;
    }

    /// <summary>
    /// Replaces the default <see cref="ToncenterRpcClient"/> with a custom <see cref="ITonRpcClient"/>.
    /// Call after <see cref="AddTonWalletPayments"/> if you need to override the RPC backend.
    /// </summary>
    public static IServiceCollection UseTonRpcClient<TClient>(this IServiceCollection services)
        where TClient : class, ITonRpcClient
    {
        ArgumentNullException.ThrowIfNull(services);

        // Remove the default registration added by AddTonWalletPayments
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITonRpcClient));
        if (descriptor is not null)
            services.Remove(descriptor);

        services.AddSingleton<ITonRpcClient, TClient>();
        return services;
    }
}
