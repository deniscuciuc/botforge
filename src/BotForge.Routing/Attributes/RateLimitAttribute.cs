namespace BotForge.Routing.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class RateLimitAttribute(int seconds) : Attribute
{
    public int Seconds { get; } = seconds;
}
