namespace BotForge.Templates;

public class PartialResolver(IMessageTemplateStore store)
{
    public string Resolve(string text, string language, HashSet<string>? visited = null)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains("{>"))
            return text;

        visited ??= [];
        var result = text;
        var searchStart = 0;

        while (true)
        {
            var start = result.IndexOf("{>", searchStart, StringComparison.Ordinal);
            if (start < 0) break;

            var end = result.IndexOf('}', start + 2);
            if (end < 0) break;

            var partialName = result.Substring(start + 2, end - start - 2).Trim();

            if (visited.Contains(partialName))
            {
                // Prevent infinite recursion
                var errorText = $"[Circular partial: {partialName}]";
                result = string.Concat(result.AsSpan(0, start), errorText, result.AsSpan(end + 1));
                searchStart = start + errorText.Length;
                continue;
            }

            var partial = store.Get(partialName);
            if (partial == null || !partial.IsPartial)
            {
                var errorText = $"[Missing partial: {partialName}]";
                result = string.Concat(result.AsSpan(0, start), errorText, result.AsSpan(end + 1));
                searchStart = start + errorText.Length;
                continue;
            }

            visited.Add(partialName);

            var partialText = partial.Text.Translations.GetValueOrDefault(language)
                              ?? partial.Text.Translations.Values.FirstOrDefault()
                              ?? string.Empty;

            // Recursively resolve partials within the partial
            partialText = Resolve(partialText, language, visited);

            result = string.Concat(result.AsSpan(0, start), partialText, result.AsSpan(end + 1));
            searchStart = start + partialText.Length;
        }

        return result;
    }

    public string ResolveLayout(MessageTemplate template, string language)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (string.IsNullOrEmpty(template.Extends))
            return template.Text.Translations.GetValueOrDefault(language)
                   ?? template.Text.Translations.Values.FirstOrDefault()
                   ?? string.Empty;

        var layout = store.Get(template.Extends);
        if (layout == null || !layout.IsLayout)
            return template.Text.Translations.GetValueOrDefault(language)
                   ?? template.Text.Translations.Values.FirstOrDefault()
                   ?? string.Empty;

        var layoutText = layout.Text.Translations.GetValueOrDefault(language)
                         ?? layout.Text.Translations.Values.FirstOrDefault()
                         ?? string.Empty;

        if (template.Blocks == null)
            return layoutText;

        // Replace {block:name} placeholders with template block content
        foreach (var (blockName, blockText) in template.Blocks)
        {
            var placeholder = $"{{block:{blockName}}}";
            var content = blockText.Translations.GetValueOrDefault(language)
                          ?? blockText.Translations.Values.FirstOrDefault()
                          ?? string.Empty;

            layoutText = layoutText.Replace(placeholder, content);
        }

        return layoutText;
    }
}
