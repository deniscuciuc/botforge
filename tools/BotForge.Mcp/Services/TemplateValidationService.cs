using System.Text;
using YamlDotNet.RepresentationModel;

namespace BotForge.Mcp.Services;

public sealed class TemplateValidationService
{
    private static readonly HashSet<string> ValidButtonTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Callback", "Url", "WebApp", "SwitchInline", "SwitchInlineCurrentChat", "Pay", "Login"
    };

    private static readonly HashSet<string> ValidParseModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "None", "Html", "Markdown", "MarkdownV2"
    };

    public TemplateValidationResult Validate(string yamlContent)
    {
        var result = new TemplateValidationResult();

        YamlDocument doc;
        try
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(yamlContent));
            if (stream.Documents.Count == 0)
            {
                result.Errors.Add("Empty YAML document");
                return result;
            }

            doc = stream.Documents[0];
        }
        catch (Exception ex)
        {
            result.Errors.Add($"YAML parse error: {ex.Message}");
            return result;
        }

        if (doc.RootNode is not YamlMappingNode root)
        {
            result.Errors.Add("Root must be a YAML mapping");
            return result;
        }

        // Check required 'name' field
        if (!TryGetScalar(root, "name", out _))
            result.Errors.Add("Missing required field: 'name'");

        // Check parseMode
        if (TryGetScalar(root, "parseMode", out var parseMode))
            if (!ValidParseModes.Contains(parseMode))
                result.Warnings.Add($"Unknown parseMode: '{parseMode}'. Valid: None, Html, Markdown, MarkdownV2");

        // Validate text translations
        var textLanguages = new HashSet<string>();
        if (TryGetMapping(root, "text", out var textNode))
        {
            foreach (var entry in textNode.Children)
                if (entry.Key is YamlScalarNode keyNode)
                    textLanguages.Add(keyNode.Value!);
            if (textLanguages.Count == 0)
                result.Warnings.Add("'text' section has no language translations");
        }
        else if (!TryGetScalar(root, "isPartial", out var isPartial) || isPartial != "true")
        {
            // text is required unless it's a partial
            result.Warnings.Add("Missing 'text' section — template has no content");
        }

        // Validate mustache balance in text values
        if (textNode is not null)
            foreach (var entry in textNode.Children)
                if (entry.Value is YamlScalarNode valueNode && valueNode.Value is not null)
                    ValidateMustacheBalance(valueNode.Value, $"text.{(entry.Key as YamlScalarNode)?.Value}", result);

        // Validate buttons
        if (TryGetSequence(root, "buttons", out var buttons))
        {
            var buttonLanguages = new HashSet<string>();
            for (var i = 0; i < buttons.Children.Count; i++)
                if (buttons.Children[i] is YamlMappingNode button)
                    ValidateButton(button, $"buttons[{i}]", textLanguages, buttonLanguages, result);
        }

        // Validate dynamic buttons
        if (TryGetSequence(root, "dynamicButtons", out var dynButtons))
            for (var i = 0; i < dynButtons.Children.Count; i++)
                if (dynButtons.Children[i] is YamlMappingNode dynButton)
                    ValidateDynamicButton(dynButton, $"dynamicButtons[{i}]", textLanguages, result);

        return result;
    }

    private void ValidateButton(YamlMappingNode button, string path, HashSet<string> textLanguages,
        HashSet<string> buttonLanguages, TemplateValidationResult result)
    {
        // Check button type
        if (TryGetScalar(button, "type", out var type))
            if (!ValidButtonTypes.Contains(type))
                result.Errors.Add(
                    $"{path}: Unknown button type '{type}'. Valid: {string.Join(", ", ValidButtonTypes)}");

        // Check value
        if (!TryGetScalar(button, "value", out _))
            result.Warnings.Add($"{path}: Missing 'value' field");

        // Check text translations consistency
        if (TryGetMapping(button, "text", out var btnText))
        {
            var btnLangs = new HashSet<string>();
            foreach (var entry in btnText.Children)
                if (entry.Key is YamlScalarNode keyNode)
                    btnLangs.Add(keyNode.Value!);

            foreach (var lang in textLanguages)
                if (!btnLangs.Contains(lang))
                    result.Warnings.Add($"{path}: Missing translation for language '{lang}' in button text");
        }
        else
        {
            result.Errors.Add($"{path}: Missing 'text' section");
        }
    }

    private void ValidateDynamicButton(YamlMappingNode dynButton, string path, HashSet<string> textLanguages,
        TemplateValidationResult result)
    {
        if (!TryGetScalar(dynButton, "name", out _))
            result.Errors.Add($"{path}: Missing required 'name' field for dynamic button slot");

        if (!TryGetScalar(dynButton, "value", out _))
            result.Warnings.Add($"{path}: Missing 'value' field");

        if (TryGetScalar(dynButton, "type", out var type))
            if (!ValidButtonTypes.Contains(type))
                result.Errors.Add($"{path}: Unknown button type '{type}'");

        // Check text translations
        if (TryGetMapping(dynButton, "text", out var btnText))
        {
            var btnLangs = new HashSet<string>();
            foreach (var entry in btnText.Children)
                if (entry.Key is YamlScalarNode keyNode)
                    btnLangs.Add(keyNode.Value!);

            foreach (var lang in textLanguages)
                if (!btnLangs.Contains(lang))
                    result.Warnings.Add($"{path}: Missing translation for language '{lang}'");
        }
    }

    private static void ValidateMustacheBalance(string text, string context, TemplateValidationResult result)
    {
        var openCount = 0;
        for (var i = 0; i < text.Length - 1; i++)
            if (text[i] == '{' && text[i + 1] == '{')
            {
                // Check for triple braces (raw) — skip
                if (i + 2 < text.Length && text[i + 2] == '{')
                    continue;
                openCount++;
                i++;
            }
            else if (text[i] == '}' && text[i + 1] == '}')
            {
                openCount--;
                i++;
            }

        if (openCount != 0)
            result.Warnings.Add($"{context}: Unbalanced mustache braces ({{{{ / }}}})");

        // Check #if / /if balance
        var ifOpens = CountOccurrences(text, "{{#if");
        var ifCloses = CountOccurrences(text, "{{/if}}");
        if (ifOpens != ifCloses)
            result.Warnings.Add(
                $"{context}: Unbalanced {{{{#if}}}}/{{{{/if}}}} blocks ({ifOpens} opens, {ifCloses} closes)");

        // Check #each / /each balance
        var eachOpens = CountOccurrences(text, "{{#each");
        var eachCloses = CountOccurrences(text, "{{/each}}");
        if (eachOpens != eachCloses)
            result.Warnings.Add(
                $"{context}: Unbalanced {{{{#each}}}}/{{{{/each}}}} blocks ({eachOpens} opens, {eachCloses} closes)");
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0, index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    private static bool TryGetScalar(YamlMappingNode node, string key, out string value)
    {
        value = "";
        var keyNode = new YamlScalarNode(key);
        if (node.Children.TryGetValue(keyNode, out var child) && child is YamlScalarNode scalar)
        {
            value = scalar.Value ?? "";
            return true;
        }

        return false;
    }

    private static bool TryGetMapping(YamlMappingNode node, string key, out YamlMappingNode mapping)
    {
        mapping = null!;
        var keyNode = new YamlScalarNode(key);
        if (node.Children.TryGetValue(keyNode, out var child) && child is YamlMappingNode m)
        {
            mapping = m;
            return true;
        }

        return false;
    }

    private static bool TryGetSequence(YamlMappingNode node, string key, out YamlSequenceNode sequence)
    {
        sequence = null!;
        var keyNode = new YamlScalarNode(key);
        if (node.Children.TryGetValue(keyNode, out var child) && child is YamlSequenceNode s)
        {
            sequence = s;
            return true;
        }

        return false;
    }
}

public sealed class TemplateValidationResult
{
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public bool IsValid => Errors.Count == 0;

    public string Format()
    {
        var sb = new StringBuilder();

        if (IsValid && Warnings.Count == 0)
        {
            sb.AppendLine("✅ Template is valid — no issues found.");
            return sb.ToString();
        }

        if (Errors.Count > 0)
        {
            sb.AppendLine("❌ ERRORS:");
            foreach (var error in Errors)
                sb.AppendLine($"  - {error}");
        }

        if (Warnings.Count > 0)
        {
            sb.AppendLine("⚠️ WARNINGS:");
            foreach (var warning in Warnings)
                sb.AppendLine($"  - {warning}");
        }

        return sb.ToString();
    }
}
