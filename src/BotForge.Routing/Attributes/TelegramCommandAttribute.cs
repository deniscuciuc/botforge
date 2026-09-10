namespace BotForge.Routing.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class TelegramCommandAttribute(string command, string description = "") : Attribute
{
    public string Command { get; } = command;
    public string Description { get; } = description;
    public string? BotId { get; init; }
    public string[] AllowedChatTypes { get; init; } = [];
    public bool RequiresUserAccount { get; init; } = true;
    public int RequiredArguments { get; init; }
    public string? UsageExample { get; init; }
}
