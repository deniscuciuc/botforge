using System.Collections.Concurrent;

namespace BotForge.Templates;

public class EmojiRegistry : IEmojiRegistry
{
    private readonly ConcurrentDictionary<string, string> _emojis = new(StringComparer.OrdinalIgnoreCase);

    public string Get(string name)
    {
        if (_emojis.TryGetValue(name, out var emoji))
            return emoji;

        throw new KeyNotFoundException(
            $"Emoji '{name}' is not registered. Register it via emojis.yml or IEmojiRegistry.Register().");
    }

    public string? GetOrDefault(string name)
    {
        return _emojis.GetValueOrDefault(name);
    }

    public bool TryGet(string name, out string emoji)
    {
        return _emojis.TryGetValue(name, out emoji!);
    }

    public IReadOnlyDictionary<string, string> GetAll()
    {
        return _emojis;
    }

    public void Register(string name, string emoji)
    {
        _emojis[name] = emoji;
    }

    public void Load(IEnumerable<KeyValuePair<string, string>> emojis)
    {
        ArgumentNullException.ThrowIfNull(emojis);

        foreach (var (name, emoji) in emojis)
            _emojis[name] = emoji;
    }

    public string ResolveEmojis(string text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains("{{emoji:"))
            return text;

        var result = text;
        var searchStart = 0;

        while (true)
        {
            var start = result.IndexOf("{{emoji:", searchStart, StringComparison.Ordinal);
            if (start < 0) break;

            var end = result.IndexOf("}}", start + 8, StringComparison.Ordinal);
            if (end < 0) break;

            var name = result.Substring(start + 8, end - start - 8);
            var replacement = GetOrDefault(name) ?? $"{{{{emoji:{name}}}}}";
            result = string.Concat(result.AsSpan(0, start), replacement, result.AsSpan(end + 2));
            searchStart = start + replacement.Length;
        }

        return result;
    }
}
