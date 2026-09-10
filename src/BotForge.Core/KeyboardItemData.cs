namespace BotForge.Core;

public class KeyboardItemData
{
    public Dictionary<string, string> Parameters { get; init; } = new();

    public string? GetValue(string key)
    {
        return Parameters.GetValueOrDefault(key);
    }

    public static KeyboardItemData FromDictionary(Dictionary<string, string> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return new KeyboardItemData { Parameters = parameters };
    }

    public static KeyboardItemData FromValues(params (string Key, string Value)[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var parameters = new Dictionary<string, string>();
        foreach (var (key, value) in values)
            parameters[key] = value;
        return new KeyboardItemData { Parameters = parameters };
    }
}
