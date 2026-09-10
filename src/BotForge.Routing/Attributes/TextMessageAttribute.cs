namespace BotForge.Routing.Attributes;

/// <summary>
/// Routes plain text messages (non-command) to a handler.
/// Optionally filters by regex pattern.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TextMessageAttribute(string? pattern = null) : Attribute
{
    /// <summary>Optional regex pattern to match against message text.</summary>
    public string? Pattern { get; } = pattern;

    /// <summary>Priority for matching. Higher values are checked first.</summary>
    public int Priority { get; init; }
}
