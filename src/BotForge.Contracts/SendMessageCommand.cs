using BotForge.Core;

namespace BotForge.Contracts;

/// <summary>
/// Command published to request the sender service to deliver a message.
/// Wraps <see cref="QueuedTelegramMessage"/> with routing metadata.
/// </summary>
public sealed record SendMessageCommand
{
    public required QueuedTelegramMessage Message { get; init; }
    public string? CorrelationId { get; init; }
    public DateTimeOffset EnqueuedAt { get; init; } = DateTimeOffset.UtcNow;
}
