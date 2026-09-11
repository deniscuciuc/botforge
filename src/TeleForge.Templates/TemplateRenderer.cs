using System.Text;
using Microsoft.Extensions.Logging;
using TeleForge.Templates.Metrics;

namespace TeleForge.Templates;

public class TemplateRenderer(
    IMessageTemplateStore store,
    IEmojiRegistry emojis,
    ConditionalEvaluator conditionals,
    LoopProcessor loops,
    PartialResolver partials,
    FormatterPipeline formatters,
    TelegramTemplateOptions options,
    ILogger<TemplateRenderer> logger,
    ILocalizationKeyResolver? localizationResolver = null)
    : ITemplateRenderer
{
    public string Render(string templateName, IDictionary<string, string> parameters, string language)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return Render(templateName, parameters, language, null);
    }

    public string Render(
        string templateName,
        IDictionary<string, string> parameters,
        string language,
        IDictionary<string, IReadOnlyList<IDictionary<string, string>>>? loopData)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var template = store.Get(templateName);
            if (template == null)
            {
                logger.LogWarning("Template '{TemplateName}' not found", templateName);
                return $"[Template not found: {templateName}]";
            }

            // 1. Resolve layout or get raw text
            string text;
            if (!string.IsNullOrEmpty(template.Extends))
                text = partials.ResolveLayout(template, language);
            else
                text = template.Text.Translations.GetValueOrDefault(language)
                       ?? template.Text.Translations.GetValueOrDefault(options.FallbackLanguage)
                       ?? template.Text.Translations.Values.FirstOrDefault()
                       ?? string.Empty;

            // 2. Resolve partials ({> partial_name})
            text = partials.Resolve(text, language);

            // 3. Resolve emojis ({{emoji:name}})
            text = emojis.ResolveEmojis(text);

            // 4. Process loops ({#each}...{/each})
            text = loops.Process(text, loopData);

            // 5. Process conditionals ({#if}...{/if})
            text = conditionals.Process(text, parameters);

            // 6. Process formatters ({Param | formatter:args})
            text = formatters.Process(text, parameters);

            // 7. Resolve localization keys ({@key} and LocalizationKey)
            if (localizationResolver is not null)
                text = localizationResolver.ResolveText(template.Text.LocalizationKey, text, language, parameters);

            // 8. Substitute parameters ({ParamName})
            text = SubstituteParameters(text, parameters);

            // 9. Clean up any remaining empty lines from conditional blocks
            text = CleanEmptyLines(text);

            sw.Stop();
            TemplateMetrics.RecordRender(templateName, language, "success", sw.Elapsed);
            return text;
        }
        catch
        {
            sw.Stop();
            TemplateMetrics.RecordRender(templateName, language, "exception", sw.Elapsed);
            throw;
        }
    }

    internal static string SubstituteParameters(string text, IDictionary<string, string> parameters)
    {
        if (parameters.Count == 0)
            return text;

        var sb = new StringBuilder(text.Length);
        var pos = 0;

        while (pos < text.Length)
        {
            var open = text.IndexOf('{', pos);
            if (open < 0)
            {
                sb.Append(text.AsSpan(pos));
                break;
            }

            var close = text.IndexOf('}', open + 1);
            if (close < 0)
            {
                sb.Append(text.AsSpan(pos));
                break;
            }

            sb.Append(text.AsSpan(pos, open - pos));

            var key = text.AsSpan(open + 1, close - open - 1);
            if (parameters is Dictionary<string, string> dict)
            {
                if (dict.TryGetValue(key.ToString(), out var value))
                    sb.Append(value);
                else
                    sb.Append(text.AsSpan(open, close - open + 1));
            }
            else if (parameters.TryGetValue(key.ToString(), out var value))
            {
                sb.Append(value);
            }
            else
            {
                sb.Append(text.AsSpan(open, close - open + 1));
            }

            pos = close + 1;
        }

        return sb.ToString();
    }

    private static string CleanEmptyLines(string text)
    {
        // Remove lines that are empty after conditional processing (only whitespace)
        var lines = text.Split('\n');
        var result = new List<string>(lines.Length);
        var prevWasEmpty = false;

        foreach (var line in lines)
        {
            var isEmpty = string.IsNullOrWhiteSpace(line);
            if (isEmpty && prevWasEmpty)
                continue;
            result.Add(line);
            prevWasEmpty = isEmpty;
        }

        return string.Join('\n', result).Trim();
    }
}
