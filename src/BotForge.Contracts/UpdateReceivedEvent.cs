using Telegram.Bot.Types;

namespace BotForge.Contracts;

/// <summary>
/// Event published when a Telegram update is received by the ingress service.
/// Used for inter-service communication when pipeline processing runs remotely.
/// </summary>
public sealed record UpdateReceivedEvent
{
    public required string BotId { get; init; }
    public required Update Update { get; init; }
    public DateTimeOffset ReceivedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? CorrelationId { get; init; }
}
