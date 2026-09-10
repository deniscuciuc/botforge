using System.Text.Json.Serialization;

namespace BotForge.Payments.Fragment.Api;

[Serializable]
internal sealed record ErrorsWrapper([property: JsonPropertyName("errors")] IEnumerable<Error>? Errors);
