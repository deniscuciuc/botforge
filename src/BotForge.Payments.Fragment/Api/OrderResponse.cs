using System.Text.Json;
using System.Text.Json.Serialization;

namespace BotForge.Payments.Fragment.Api;

[Serializable]
internal sealed record OrderResponse(
    [property: JsonPropertyName("success")]
    bool? Success,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("receiver")]
    string Receiver,
    [property: JsonPropertyName("goods_quantity")]
    int? GoodsQuantity,
    [property: JsonPropertyName("sender")] Sender? Sender,
    [property: JsonPropertyName("ton_price")]
    string? TonPrice,
    [property: JsonPropertyName("ref_id")] string? RefId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("error")] JsonElement? Error,
    [property: JsonPropertyName("created_at")]
    DateTime CreatedAt
);
