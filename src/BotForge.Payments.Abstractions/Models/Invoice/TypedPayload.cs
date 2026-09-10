namespace BotForge.Payments.Abstractions;

/// <summary>
/// Base class for typed invoice payloads. Subclasses define a prefix and
/// are serialized as "PREFIX:field1:field2:..." for the Telegram invoice payload string.
/// </summary>
public abstract record TypedPayload
{
    public abstract string Prefix { get; }

    /// <summary>
    /// Serialize payload fields (excluding prefix) into colon-separated segments.
    /// Default implementation returns empty (prefix-only payload).
    /// </summary>
    public virtual string[] SerializeFields()
    {
        return [];
    }

    /// <summary>
    /// Serialize the complete payload string including prefix.
    /// </summary>
    public string Serialize()
    {
        var fields = SerializeFields();
        return fields.Length == 0
            ? Prefix
            : $"{Prefix}:{string.Join(':', fields)}";
    }
}
