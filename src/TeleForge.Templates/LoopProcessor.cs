using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TeleForge.Templates;

public partial class LoopProcessor
{
    public string Process(string text, IDictionary<string, IReadOnlyList<IDictionary<string, string>>>? loopData)
    {
        if (string.IsNullOrEmpty(text) || loopData == null || loopData.Count == 0)
            return text;

        var result = text;

        while (true)
        {
            var match = EachBlockRegex().Match(result);
            if (!match.Success) break;

            var collectionName = match.Groups["collection"].Value.Trim();
            var body = match.Groups["body"].Value;

            if (!loopData.TryGetValue(collectionName, out var items))
            {
                // Remove the block if no data
                result = string.Concat(
                    result.AsSpan(0, match.Index),
                    result.AsSpan(match.Index + match.Length));
                continue;
            }

            var countStr = items.Count.ToString(CultureInfo.InvariantCulture);
            var sb = new StringBuilder(body.Length * Math.Min(items.Count, 128));

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var isFirst = i == 0 ? "true" : "false";
                var isLast = i == items.Count - 1 ? "true" : "false";
                var indexStr = (i + 1).ToString(CultureInfo.InvariantCulture);
                var index0Str = i.ToString(CultureInfo.InvariantCulture);

                // Single-pass scan of body
                var pos = 0;
                while (pos < body.Length)
                {
                    var open = body.IndexOf('{', pos);
                    if (open < 0)
                    {
                        sb.Append(body.AsSpan(pos));
                        break;
                    }

                    var close = body.IndexOf('}', open + 1);
                    if (close < 0)
                    {
                        sb.Append(body.AsSpan(pos));
                        break;
                    }

                    sb.Append(body.AsSpan(pos, open - pos));

                    var key = body.AsSpan(open + 1, close - open - 1);
                    if (key.SequenceEqual("Index"))
                        sb.Append(indexStr);
                    else if (key.SequenceEqual("Index0"))
                        sb.Append(index0Str);
                    else if (key.SequenceEqual("IsFirst"))
                        sb.Append(isFirst);
                    else if (key.SequenceEqual("IsLast"))
                        sb.Append(isLast);
                    else if (key.SequenceEqual("Count"))
                        sb.Append(countStr);
                    else if (item.TryGetValue(key.ToString(), out var value))
                        sb.Append(value);
                    else
                        sb.Append(body.AsSpan(open, close - open + 1));

                    pos = close + 1;
                }
            }

            result = string.Concat(
                result.AsSpan(0, match.Index),
                sb.ToString(),
                result.AsSpan(match.Index + match.Length));
        }

        return result;
    }

    [GeneratedRegex(@"\{#each\s+(?<collection>[^}]+)\}(?<body>.*?)\{/each\}", RegexOptions.Singleline)]
    private static partial Regex EachBlockRegex();
}
