namespace BotForge.Routing.Abstractions;

/// <summary>
/// Represents a single step in a multi-step conversation flow.
/// </summary>
public class ConversationStep
{
    public required string StepId { get; init; }
    public required Type HandlerType { get; init; }
    public Dictionary<string, string> Data { get; init; } = [];
    public DateTimeOffset? ExpiresAt { get; init; }
}
