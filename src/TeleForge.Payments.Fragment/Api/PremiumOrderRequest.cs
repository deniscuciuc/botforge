using System.Text.Json.Serialization;

namespace TeleForge.Payments.Fragment.Api;

[Serializable]
internal sealed record PremiumOrderRequest(
    [property: JsonPropertyName("username")]
    string Username,
    [property: JsonPropertyName("months")] int Months,
    [property: JsonPropertyName("show_sender")]
    bool ShowSender = false,
    [property: JsonPropertyName("webhook_url")]
    string? WebhookUrl = null
);
