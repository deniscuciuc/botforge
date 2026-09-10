using BotForge.Core;
using BotForge.Payments.Abstractions;
using BotForge.Payments.AntiFraud;
using BotForge.Payments.Checkout;
using BotForge.Payments.Commerce;
using BotForge.Payments.Gift;
using BotForge.Payments.Invoice;
using BotForge.Payments.Metrics;
using BotForge.Payments.PaidMedia;
using BotForge.Payments.Payout;
using BotForge.Payments.Refund;
using BotForge.Payments.Revenue;
using BotForge.Payments.Router;
using BotForge.Payments.Subscription;
using BotForge.Payments.Transaction;
using BotForge.Payments.Wallet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BotForge.Payments.Extensions;

public static class PaymentsServiceCollectionExtensions
{
    /// <summary>
    /// Registers BotForge.Payments services.
    /// </summary>
    public static IServiceCollection AddTelegramPayments(
        this IServiceCollection services,
        Action<PaymentOptions>? configureOptions = null,
        Action<PaymentHandlerRegistry>? configureHandlers = null)
    {
        if (configureOptions != null)
            services.Configure(configureOptions);
        else
            services.Configure<PaymentOptions>(_ => { });

        var registry = new PaymentHandlerRegistry();
        configureHandlers?.Invoke(registry);
        services.AddSingleton(registry);

        services.AddSingleton<IInvoiceService, InvoiceService>();
        services.AddSingleton<PreCheckoutPipeline>();
        services.AddSingleton<PaymentProcessingPipeline>();
        services.AddSingleton<RefundPipeline>();
        services.AddSingleton<IPaymentRouter, PaymentRouter>();
        services.AddSingleton<IRefundService, RefundService>();

        services.AddSingleton<IStarTransactionService, StarTransactionService>();
        services.AddSingleton<IRevenueService, RevenueService>();

        services.AddSingleton<IGiftService, GiftService>();

        services.AddSingleton<IPaidMediaService, PaidMediaService>();

        services.AddSingleton<ISubscriptionService, SubscriptionService>();

        services.AddSingleton<AntiFraudPipeline>();

        services.AddSingleton<IPayoutPipeline, PayoutPipeline>();
        services.AddSingleton<IPayoutProvider, ManualPayoutProvider>();

        services.TryAddSingleton<IPaymentMetrics>(_ =>
            TelegramMetricsRuntime.PaymentsEnabled
                ? new MeterPaymentMetrics()
                : NullPaymentMetrics.Instance);

        return services;
    }

    /// <summary>
    /// Registers a pre-checkout validator for a specific payload prefix.
    /// </summary>
    public static PaymentHandlerRegistry AddValidator<T>(this PaymentHandlerRegistry registry, string prefix)
        where T : class, IPreCheckoutValidator
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.RegisterValidator(prefix, typeof(T));
        return registry;
    }

    /// <summary>
    /// Registers a payment processor for a specific payload prefix.
    /// </summary>
    public static PaymentHandlerRegistry AddProcessor<T>(this PaymentHandlerRegistry registry, string prefix)
        where T : class, IPaymentProcessor
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.RegisterProcessor(prefix, typeof(T));
        return registry;
    }

    /// <summary>
    /// Registers a refund processor for a specific payload prefix.
    /// </summary>
    public static PaymentHandlerRegistry AddRefundProcessor<T>(this PaymentHandlerRegistry registry, string prefix)
        where T : class, IRefundProcessor
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.RegisterRefundProcessor(prefix, typeof(T));
        return registry;
    }

    /// <summary>
    /// Adds the background payout worker.
    /// </summary>
    public static IServiceCollection AddPayoutWorker(this IServiceCollection services)
    {
        services.AddHostedService<PayoutWorker>();
        return services;
    }

    /// <summary>
    /// Adds the background transaction sync worker.
    /// </summary>
    public static IServiceCollection AddTransactionSyncWorker(this IServiceCollection services)
    {
        services.AddHostedService<TransactionSyncWorker>();
        return services;
    }

    /// <summary>
    /// Registers a global pre-checkout validator.
    /// </summary>
    public static IServiceCollection AddGlobalPreCheckoutValidator<T>(this IServiceCollection services)
        where T : GlobalPreCheckoutValidator
    {
        services.AddSingleton<GlobalPreCheckoutValidator, T>();
        return services;
    }

    /// <summary>
    /// Registers an anti-fraud check.
    /// </summary>
    public static IServiceCollection AddAntiFraudCheck<T>(this IServiceCollection services)
        where T : class, IAntiFraudCheck
    {
        services.AddSingleton<IAntiFraudCheck, T>();
        return services;
    }

    // -------------------------------------------------------------------------
    // Cross-rail commerce (gifts + wallet payments)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Enables the unified purchase fulfillment pipeline.
    /// Required before calling <see cref="AddGiftInboundSupport"/> or
    /// <see cref="AddWalletPaymentSupport"/>.
    /// The caller must also register <see cref="ICommerceStore"/> as a singleton.
    /// </summary>
    public static IServiceCollection AddPurchaseFulfillment(
        this IServiceCollection services,
        Action<PurchaseFulfillmentRegistry>? configureHandlers = null)
    {
        var registry = new PurchaseFulfillmentRegistry();
        configureHandlers?.Invoke(registry);
        services.AddSingleton(registry);
        services.AddSingleton<PurchaseFulfillmentPipeline>();
        return services;
    }

    /// <summary>
    /// Registers a fulfillment handler for a specific payload prefix.
    /// </summary>
    public static PurchaseFulfillmentRegistry AddFulfillmentHandler<T>(
        this PurchaseFulfillmentRegistry registry, string prefix)
        where T : class, IPurchaseFulfillmentHandler
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(prefix, typeof(T));
        return registry;
    }

    /// <summary>
    /// Enables inbound gift-as-payment processing.
    /// Requires <see cref="AddPurchaseFulfillment"/> and a registered <see cref="IGiftIntakeStore"/>.
    /// </summary>
    public static IServiceCollection AddGiftInboundSupport(this IServiceCollection services)
    {
        services.AddSingleton<IGiftIntakeService, GiftIntakeService>();
        return services;
    }

    /// <summary>
    /// Enables wallet-based payment sessions (TON direct-address, TON Connect, custodial).
    /// Requires <see cref="AddPurchaseFulfillment"/> and a registered <see cref="IWalletPaymentStore"/>.
    /// </summary>
    public static IServiceCollection AddWalletPaymentSupport(this IServiceCollection services)
    {
        services.AddSingleton<IWalletPaymentService, WalletPaymentService>();
        return services;
    }
}
