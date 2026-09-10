namespace BotForge.Routing.Abstractions;

public interface ICallbackDataSerializer
{
    string Serialize<T>(T data) where T : ICallbackData;
    T? Deserialize<T>(string callbackData) where T : ICallbackData;
    object? Deserialize(string callbackData, Type type);
    bool TryMatch(string callbackData, string pattern, out IDictionary<string, string> parameters);
}

public interface ICallbackData
{
    static abstract string Pattern { get; }
}
