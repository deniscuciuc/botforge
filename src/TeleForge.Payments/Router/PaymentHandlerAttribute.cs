namespace TeleForge.Payments.Router;

/// <summary>
/// Attribute to mark a payment handler with its payload prefix for discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class PaymentHandlerAttribute(string prefix) : Attribute
{
    public string Prefix { get; } = prefix;
}
