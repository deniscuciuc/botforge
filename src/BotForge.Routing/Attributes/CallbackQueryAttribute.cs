namespace BotForge.Routing.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CallbackQueryAttribute(string pattern, string description = "") : Attribute
{
    public string Pattern { get; } = pattern;
    public string Description { get; } = description;
    public bool IsRegex { get; init; }
}
