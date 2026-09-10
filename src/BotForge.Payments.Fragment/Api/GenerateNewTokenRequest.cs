using System.Text.Json.Serialization;

namespace BotForge.Payments.Fragment.Api;

[Serializable]
internal sealed record GenerateNewTokenRequest(
    [property: JsonPropertyName("api_key")]
    Guid? ApiKey,
    [property: JsonPropertyName("phone_number")]
    string PhoneNumber,
    [property: JsonPropertyName("mnemonics")]
    IReadOnlyList<string> Mnemonics,
    [property: JsonPropertyName("version")]
    string? Version = null,
    [property: JsonPropertyName("name")] string? Name = null
);
