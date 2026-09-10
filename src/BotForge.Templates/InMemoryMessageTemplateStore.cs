using System.Collections.Concurrent;

namespace BotForge.Templates;

public class InMemoryMessageTemplateStore : IMessageTemplateStore
{
    private readonly ConcurrentDictionary<string, MessageTemplate> _templates = new(StringComparer.OrdinalIgnoreCase);

    public MessageTemplate? Get(string name)
    {
        return _templates.GetValueOrDefault(name);
    }

    public IReadOnlyList<MessageTemplate> GetAll()
    {
        return _templates.Values.ToList();
    }

    public void Load(IEnumerable<MessageTemplate> templates)
    {
        ArgumentNullException.ThrowIfNull(templates);

        foreach (var template in templates)
            _templates[template.Name] = template;
    }

    public void AddOrReplace(MessageTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        _templates[template.Name] = template;
    }

    public bool Remove(string name)
    {
        return _templates.TryRemove(name, out _);
    }
}
