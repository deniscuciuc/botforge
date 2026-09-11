using TeleForge.Payments.Abstractions;

namespace TeleForge.Payments.Invoice;

/// <summary>
/// Typed payload serialization/deserialization.
/// Payloads follow the format: "PREFIX:field1:field2:..."
/// </summary>
public static class PayloadSerializer
{
    /// <summary>
    /// Extract the prefix from a raw payload string.
    /// </summary>
    public static string GetPrefix(string rawPayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawPayload);
        var colonIndex = rawPayload.IndexOf(':');
        return colonIndex < 0 ? rawPayload : rawPayload[..colonIndex];
    }

    /// <summary>
    /// Extract the field segments (everything after the prefix) from a raw payload.
    /// </summary>
    public static string[] GetFields(string rawPayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawPayload);
        var colonIndex = rawPayload.IndexOf(':');
        if (colonIndex < 0 || colonIndex == rawPayload.Length - 1)
            return [];
        return rawPayload[(colonIndex + 1)..].Split(':');
    }

    /// <summary>
    /// Serialize a typed payload into a raw payload string.
    /// </summary>
    public static string Serialize(TypedPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return payload.Serialize();
    }

    /// <summary>
    /// Deserialize a raw payload into a typed payload using a factory function.
    /// The factory receives the field segments (everything after the prefix).
    /// </summary>
    public static T Deserialize<T>(string rawPayload, Func<string[], T> factory) where T : TypedPayload
    {
        ArgumentNullException.ThrowIfNull(factory);

        var fields = GetFields(rawPayload);
        return factory(fields);
    }
}
