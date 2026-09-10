using System.Text.Json.Serialization;

namespace BotForge.Payments.Fragment.Api;

[Serializable]
internal sealed record StarsOrderRequest(
    [property: JsonPropertyName("username")]
    string Username,
    [property: JsonPropertyName("quantity")]
    int Quantity,
    [property: JsonPropertyName("show_sender")]
    bool ShowSender = false,
    [property: JsonPropertyName("webhook_url")]
    string? WebhookUrl = null
);
