using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using TeleForge.Core;

namespace TeleForge.Observability;

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
                meterNames.Add("TeleForge.Messaging");
            if (options.EnableRoutingMetrics)
                meterNames.Add("TeleForge.Routing");
            if (options.EnableConsumerMetrics)
                meterNames.Add("TeleForge.Consumer");
            if (options.EnableTemplateMetrics)
                meterNames.Add("TeleForge.Templates");
            if (options.EnablePaymentMetrics)
                meterNames.Add("TeleForge.Payments");

            if (meterNames.Count > 0)
                builder.AddMeter(meterNames.ToArray());
        }

        if (options.EnablePrometheusExporter)
            builder.AddPrometheusHttpListener(httpListener =>
            {
                // The exporter replaced its UriPrefixes property with Host/Port plus this
                // escape hatch. Prefixes stay the public option here because they express
                // things Host/Port cannot, such as binding to every interface with "+".
                httpListener.ConfigureHttpListener = (_, listener) =>
                {
                    listener.Prefixes.Clear();
                    foreach (var prefix in options.PrometheusUriPrefixes)
                        listener.Prefixes.Add(prefix);
                };
            });
    }
}
