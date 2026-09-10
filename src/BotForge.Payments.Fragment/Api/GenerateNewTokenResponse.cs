using System.Text.Json.Serialization;

namespace BotForge.Payments.Fragment.Api;

[Serializable]
internal sealed record GenerateNewTokenResponse([property: JsonPropertyName("token")] string Token);
