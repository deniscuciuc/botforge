using System.Text.Json.Serialization;

namespace BotForge.Payments.Fragment.Api;

[Serializable]
internal sealed record Error(
    [property: JsonPropertyName("error")] string? Message,
    [property: JsonPropertyName("code")] string? Code
);
