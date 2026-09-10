using System.Net;

namespace BotForge.ApiProbe.Core;

public sealed class RequestRecord
{
    public required int Index { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required TimeSpan Latency { get; init; }
    public required HttpStatusCode HttpStatus { get; init; }
    public required bool IsRateLimited { get; init; }
    public int? RetryAfterSeconds { get; init; }
    public string? ErrorMessage { get; init; }
    public required long TargetChatId { get; init; }
    public required string BotName { get; init; }
    public int? MessageId { get; init; }
    public ApiMethod ApiMethod { get; init; } = ApiMethod.SendMessage;
    public long? PayloadBytes { get; init; }
}
