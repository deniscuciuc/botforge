using System.Text.Json.Serialization;

namespace BotForge.Payments.Fragment.Api;

[Serializable]
internal sealed record Sender(
    [property: JsonPropertyName("phone_number")]
    string PhoneNumber,
    [property: JsonPropertyName("name")] string? Name
);
