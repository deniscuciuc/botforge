namespace TeleForge.Observability;

public sealed class TelegramObservabilityOptions
{
    public bool EnableMetrics { get; set; } = true;
    public bool EnableMessagingMetrics { get; set; } = true;
    public bool EnableRoutingMetrics { get; set; } = true;
    public bool EnableConsumerMetrics { get; set; } = true;
    public bool EnableTemplateMetrics { get; set; } = true;
    public bool EnablePaymentMetrics { get; set; } = true;

    public bool EnablePrometheusExporter { get; set; } = true;

    public string[] PrometheusUriPrefixes { get; set; } = ["http://localhost:9464/"];
}
