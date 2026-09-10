namespace BotForge.Templates;

public interface IEmojiRegistry
{
    string Get(string name);
    string? GetOrDefault(string name);
    bool TryGet(string name, out string emoji);
    IReadOnlyDictionary<string, string> GetAll();
    void Register(string name, string emoji);
    void Load(IEnumerable<KeyValuePair<string, string>> emojis);
    string ResolveEmojis(string text);
}
