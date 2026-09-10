using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace BotForge.Core;

public static partial class TelegramTextEntityCompiler
{
    public static RenderedMessageText? CompileNullable(string? text, TelegramParseMode parseMode)
    {
        if (text is null)
            return null;

        if (text.Length == 0)
            return new RenderedMessageText { Text = string.Empty };

        if (parseMode == TelegramParseMode.Html && text.Contains("<tg-emoji", StringComparison.OrdinalIgnoreCase))
            return CompileHtml(text);

        if (text.Contains("<tg-emoji", StringComparison.OrdinalIgnoreCase))
            return CompileCustomEmojiOnly(text);

        return new RenderedMessageText { Text = text };
    }

    private static RenderedMessageText CompileCustomEmojiOnly(string text)
    {
        var plain = new StringBuilder(text.Length);
        var entities = new List<MessageEntity>();
        var cursor = 0;

        foreach (Match match in CustomEmojiRegex().Matches(text))
        {
            plain.Append(text.AsSpan(cursor, match.Index - cursor));

            var attrs = ParseAttributes(match.Groups["attrs"].Value);
            if (!attrs.TryGetValue("emoji-id", out var customEmojiId))
            {
                plain.Append(match.Value);
                cursor = match.Index + match.Length;
                continue;
            }

            var fallback = ResolveEmojiFallback(match);
            var offset = plain.Length;

            plain.Append(fallback);
            entities.Add(new MessageEntity
            {
                Type = MessageEntityType.CustomEmoji,
                Offset = offset,
                Length = fallback.Length,
                CustomEmojiId = customEmojiId
            });

            cursor = match.Index + match.Length;
        }

        plain.Append(text.AsSpan(cursor));

        return new RenderedMessageText
        {
            Text = plain.ToString(),
            Entities = entities.Count > 0 ? entities : null
        };
    }

    private static RenderedMessageText CompileHtml(string html)
    {
        var plain = new StringBuilder(html.Length);
        var entities = new List<MessageEntity>();
        var stack = new List<OpenEntity>();
        var cursor = 0;

        foreach (Match match in HtmlTagRegex().Matches(html))
        {
            AppendDecodedText(html.AsSpan(cursor, match.Index - cursor), plain);

            var isClosing = match.Groups["closing"].Success;
            var rawName = match.Groups["name"].Value;
            var name = rawName.ToLowerInvariant();
            var attrs = ParseAttributes(match.Groups["attrs"].Value);
            var isSelfClosing = match.Groups["selfclosing"].Success || name == "br";

            if (isClosing)
                CloseTag(stack, entities, plain, name);
            else if (isSelfClosing)
                HandleSelfClosingTag(name, attrs, plain, entities);
            else
                RegisterOpenTag(stack, plain, name, attrs);

            cursor = match.Index + match.Length;
        }

        AppendDecodedText(html.AsSpan(cursor), plain);

        while (stack.Count > 0)
        {
            var openTag = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            AddEntity(entities, openTag, plain.Length, plain);
        }

        return new RenderedMessageText
        {
            Text = plain.ToString(),
            Entities = entities.Count > 0
                ? entities.OrderBy(entity => entity.Offset).ThenByDescending(entity => entity.Length).ToArray()
                : null
        };
    }

    private static void HandleSelfClosingTag(
        string name,
        Dictionary<string, string> attrs,
        StringBuilder plain,
        List<MessageEntity> entities)
    {
        if (name == "br")
        {
            plain.Append('\n');
            return;
        }

        if (name != "tg-emoji" || !attrs.TryGetValue("emoji-id", out var customEmojiId))
            return;

        var fallback = ResolveEmojiFallback(attrs, null);
        var offset = plain.Length;
        plain.Append(fallback);

        entities.Add(new MessageEntity
        {
            Type = MessageEntityType.CustomEmoji,
            Offset = offset,
            Length = fallback.Length,
            CustomEmojiId = customEmojiId
        });
    }

    private static void RegisterOpenTag(List<OpenEntity> stack, StringBuilder plain, string name,
        Dictionary<string, string> attrs)
    {
        var openTag = name switch
        {
            "b" or "strong" => new OpenEntity(name, MessageEntityType.Bold, plain.Length),
            "i" or "em" => new OpenEntity(name, MessageEntityType.Italic, plain.Length),
            "u" or "ins" => new OpenEntity(name, MessageEntityType.Underline, plain.Length),
            "s" or "strike" or "del" => new OpenEntity(name, MessageEntityType.Strikethrough, plain.Length),
            "tg-spoiler" => new OpenEntity(name, MessageEntityType.Spoiler, plain.Length),
            "code" => new OpenEntity(name, MessageEntityType.Code, plain.Length),
            "pre" => new OpenEntity(
                name,
                MessageEntityType.Pre,
                plain.Length,
                Language: attrs.GetValueOrDefault("language")),
            "a" when attrs.TryGetValue("href", out var href) => new OpenEntity(name, MessageEntityType.TextLink,
                plain.Length, href),
            "blockquote" => new OpenEntity(
                name,
                attrs.ContainsKey("expandable") ? MessageEntityType.ExpandableBlockquote : MessageEntityType.Blockquote,
                plain.Length),
            "tg-emoji" when attrs.TryGetValue("emoji-id", out var customEmojiId) => new OpenEntity(
                name,
                MessageEntityType.CustomEmoji,
                plain.Length,
                CustomEmojiId: customEmojiId,
                Fallback: ResolveEmojiFallback(attrs, null)),
            _ => null
        };

        if (openTag != null)
            stack.Add(openTag);
    }

    private static void CloseTag(List<OpenEntity> stack, List<MessageEntity> entities, StringBuilder plain, string name)
    {
        for (var index = stack.Count - 1; index >= 0; index--)
        {
            var openTag = stack[index];
            if (!TagNamesMatch(openTag.Name, name))
                continue;

            stack.RemoveAt(index);
            AddEntity(entities, openTag, plain.Length, plain);
            return;
        }
    }

    private static void AddEntity(List<MessageEntity> entities, OpenEntity openTag, int endOffset, StringBuilder plain)
    {
        var length = endOffset - openTag.Offset;
        if (length == 0 && openTag.Type == MessageEntityType.CustomEmoji)
        {
            var fallback = openTag.Fallback ?? "\uFFFC";
            plain.Append(fallback);
            length = fallback.Length;
        }

        if (length <= 0)
            return;

        entities.Add(new MessageEntity
        {
            Type = openTag.Type,
            Offset = openTag.Offset,
            Length = length,
            Url = openTag.Url,
            Language = openTag.Language,
            CustomEmojiId = openTag.CustomEmojiId
        });
    }

    private static void AppendDecodedText(ReadOnlySpan<char> value, StringBuilder plain)
    {
        if (value.IsEmpty)
            return;

        plain.Append(WebUtility.HtmlDecode(value.ToString()));
    }

    private static Dictionary<string, string> ParseAttributes(string value)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in HtmlAttributeRegex().Matches(value))
        {
            var key = match.Groups["name"].Value;
            var attrValue = match.Groups["doubleQuoted"].Success
                ? match.Groups["doubleQuoted"].Value
                : match.Groups["singleQuoted"].Value;
            attributes[key] = WebUtility.HtmlDecode(attrValue);
        }

        foreach (Match match in HtmlBooleanAttributeRegex().Matches(value))
        {
            var key = match.Groups["name"].Value;
            if (!attributes.ContainsKey(key))
                attributes[key] = string.Empty;
        }

        return attributes;
    }

    private static string ResolveEmojiFallback(Match match)
    {
        var attrs = ParseAttributes(match.Groups["attrs"].Value);
        var innerText = match.Groups["content"].Success ? WebUtility.HtmlDecode(match.Groups["content"].Value) : null;
        return ResolveEmojiFallback(attrs, innerText);
    }

    private static string ResolveEmojiFallback(Dictionary<string, string> attrs, string? innerText)
    {
        if (!string.IsNullOrEmpty(innerText))
            return innerText;

        if (attrs.TryGetValue("fallback", out var fallback) && !string.IsNullOrEmpty(fallback))
            return fallback;

        return "\uFFFC";
    }

    private static bool TagNamesMatch(string openTagName, string closeTagName)
    {
        if (string.Equals(openTagName, closeTagName, StringComparison.OrdinalIgnoreCase))
            return true;

        return (openTagName, closeTagName) switch
        {
            ("strong", "b") => true,
            ("b", "strong") => true,
            ("em", "i") => true,
            ("i", "em") => true,
            ("ins", "u") => true,
            ("u", "ins") => true,
            ("strike", "s") => true,
            ("s", "strike") => true,
            ("del", "s") => true,
            ("s", "del") => true,
            _ => false
        };
    }

    private sealed record OpenEntity(
        string Name,
        MessageEntityType Type,
        int Offset,
        string? Url = null,
        string? Language = null,
        string? CustomEmojiId = null,
        string? Fallback = null);

    [GeneratedRegex(@"<(?<closing>/)?(?<name>[a-zA-Z][a-zA-Z0-9-]*)(?<attrs>[^>]*?)(?<selfclosing>/)?>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(
        @"(?<name>[a-zA-Z_:][a-zA-Z0-9_:\-]*)\s*=\s*(?:""(?<doubleQuoted>[^""]*)""|'(?<singleQuoted>[^']*)')",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex HtmlAttributeRegex();

    [GeneratedRegex(@"\b(?<name>[a-zA-Z_:][a-zA-Z0-9_:\-]*)\b(?!\s*=)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex HtmlBooleanAttributeRegex();

    [GeneratedRegex(@"<tg-emoji(?<attrs>[^>]*?)(?:>(?<content>.*?)</tg-emoji>|/>)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CustomEmojiRegex();
}
