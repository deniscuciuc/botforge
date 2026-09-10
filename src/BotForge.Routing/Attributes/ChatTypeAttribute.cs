namespace BotForge.Routing.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ChatTypeAttribute(params string[] chatTypes) : Attribute
{
    public string[] ChatTypes { get; } = chatTypes;
}
