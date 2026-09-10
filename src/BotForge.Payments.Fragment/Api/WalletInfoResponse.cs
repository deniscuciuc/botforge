using System.Text.Json.Serialization;

namespace BotForge.Payments.Fragment.Api;

[Serializable]
internal sealed record WalletInfoResponse(
    [property: JsonPropertyName("balance")]
    string Balance);
