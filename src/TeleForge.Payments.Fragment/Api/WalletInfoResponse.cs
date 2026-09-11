using System.Text.Json.Serialization;

namespace TeleForge.Payments.Fragment.Api;

[Serializable]
internal sealed record WalletInfoResponse(
    [property: JsonPropertyName("balance")]
    string Balance);
