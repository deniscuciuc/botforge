using BotForge.Core;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;

namespace BotForge.Observability;

public static class TelegramObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramObservability(
        this IServiceCollection services,
        Action<TelegramObservabilityOptions>? configure = null)
    {
        var options = new TelegramObservabilityOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        var metricsEnabled = options.EnableMetrics;
        TelegramMetricsRuntime.Configure(
            metricsEnabled && options.EnableMessagingMetrics,
            metricsEnabled && options.EnableRoutingMetrics,
            metricsEnabled && options.EnableConsumerMetrics,
            metricsEnabled && options.EnableTemplateMetrics,
            metricsEnabled && options.EnablePaymentMetrics);

        services.AddOpenTelemetry()
            .WithMetrics(builder => ConfigureMetrics(builder, options));

        return services;
    }

    private static void ConfigureMetrics(MeterProviderBuilder builder, TelegramObservabilityOptions options)
    {
        if (options.EnableMetrics)
        {
            var meterNames = new List<string>(5);

            if (options.EnableMessagingMetrics)
                meterNames.Add("BotForge.Messaging");
            if (options.EnableRoutingMetrics)
                meterNames.Add("BotForge.Routing");
            if (options.EnableConsumerMetrics)
                meterNames.Add("BotForge.Consumer");
            if (options.EnableTemplateMetrics)
                meterNames.Add("BotForge.Templates");
            if (options.EnablePaymentMetrics)
                meterNames.Add("BotForge.Payments");

            if (meterNames.Count > 0)
                builder.AddMeter(meterNames.ToArray());
        }

        if (options.EnablePrometheusExporter)
            builder.AddPrometheusHttpListener(httpListener =>
            {
                httpListener.UriPrefixes = options.PrometheusUriPrefixes;
            });
    }
}
