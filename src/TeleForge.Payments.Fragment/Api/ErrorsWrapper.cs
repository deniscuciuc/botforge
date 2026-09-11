using System.Text.Json.Serialization;

namespace TeleForge.Payments.Fragment.Api;

[Serializable]
internal sealed record ErrorsWrapper([property: JsonPropertyName("errors")] IEnumerable<Error>? Errors);
