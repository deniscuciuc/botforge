namespace TeleForge.Routing.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AuthorizeAttribute(string policy) : Attribute
{
    public string Policy { get; } = policy;
}
