using System.Text.Json.Serialization;

namespace TeleForge.Payments.Fragment.Api;

[Serializable]
internal sealed record UserInfoResponse(
    [property: JsonPropertyName("username")]
    string Username,
    [property: JsonPropertyName("photo")] bool? Photo,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("has_premium")]
    bool? HasPremium
);
