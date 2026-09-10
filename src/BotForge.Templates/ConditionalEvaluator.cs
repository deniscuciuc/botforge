using System.Text.RegularExpressions;

namespace BotForge.Templates;

public partial class ConditionalEvaluator
{
    public string Process(string text, IDictionary<string, string> parameters)
    {
        return string.IsNullOrEmpty(text)
            ? text
            : ProcessConditionalBlocks(text, parameters);
    }

    /// <summary>
    /// Process {#if ...}...{#else}...{/if} and {#if ...}...{/if}
    /// </summary>
    private string ProcessConditionalBlocks(string text, IDictionary<string, string> parameters)
    {
        var result = text;

        // Process nested conditionals from innermost to outermost
        while (true)
        {
            var match = IfBlockRegex().Match(result);
            if (!match.Success) break;

            var condition = match.Groups["condition"].Value.Trim();
            var trueBlock = match.Groups["true"].Value;
            var hasElse = match.Groups["else"].Success;
            var falseBlock = hasElse ? match.Groups["else"].Value : string.Empty;

            var conditionResult = EvaluateCondition(condition, parameters);
            var replacement = conditionResult ? trueBlock : falseBlock;

            result = string.Concat(
                result.AsSpan(0, match.Index),
                replacement,
                result.AsSpan(match.Index + match.Length));
        }

        return result;
    }

    public bool EvaluateCondition(string condition, IDictionary<string, string> parameters)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(parameters);

        // Handle logical operators
        if (condition.Contains("&&"))
        {
            var parts = condition.Split("&&", 2);
            return EvaluateCondition(parts[0].Trim(), parameters) &&
                   EvaluateCondition(parts[1].Trim(), parameters);
        }

        if (condition.Contains("||"))
        {
            var parts = condition.Split("||", 2);
            return EvaluateCondition(parts[0].Trim(), parameters) ||
                   EvaluateCondition(parts[1].Trim(), parameters);
        }

        // Handle negation
        if (condition.StartsWith('!') && !condition.Contains(' '))
        {
            var paramName = condition[1..];
            return !parameters.TryGetValue(paramName, out var val) ||
                   string.IsNullOrEmpty(val) ||
                   val.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                   val == "0";
        }

        // Handle exists / !exists
        if (condition.EndsWith(" exists", StringComparison.OrdinalIgnoreCase))
        {
            var paramName = condition[..^7].Trim();
            return parameters.TryGetValue(paramName, out var val) && !string.IsNullOrEmpty(val);
        }

        if (condition.EndsWith(" !exists", StringComparison.OrdinalIgnoreCase))
        {
            var paramName = condition[..^8].Trim();
            return !parameters.TryGetValue(paramName, out var val) || string.IsNullOrEmpty(val);
        }

        // Handle comparison operators
        if (TryParseComparison(condition, out var left, out var op, out var right))
        {
            parameters.TryGetValue(left, out var paramValue);
            paramValue ??= string.Empty;
            return EvaluateComparison(paramValue, op, right);
        }

        // Simple truthy check: {#if ParamName}
        if (parameters.TryGetValue(condition, out var value))
            return !string.IsNullOrEmpty(value) &&
                   !value.Equals("false", StringComparison.OrdinalIgnoreCase) &&
                   value != "0";

        return false;
    }

    private static bool TryParseComparison(string condition, out string left, out string op, out string right)
    {
        string[] operators = ["!=", ">=", "<=", "==", "=", ">", "<", " in ", " contains "];

        foreach (var candidate in operators)
        {
            var index = condition.IndexOf(candidate, StringComparison.OrdinalIgnoreCase);

            if (index <= 0) continue;

            left = condition[..index].Trim();
            op = candidate.Trim();
            right = condition[(index + candidate.Length)..].Trim();
            return true;
        }

        left = op = right = string.Empty;
        return false;
    }

    private static bool EvaluateComparison(string paramValue, string op, string right)
    {
        switch (op)
        {
            case "=" or "==":
                return paramValue.Equals(right, StringComparison.OrdinalIgnoreCase);

            case "!=":
                return !paramValue.Equals(right, StringComparison.OrdinalIgnoreCase);

            case ">" when TryParseNumbers(paramValue, right, out var l, out var r):
                return l > r;

            case ">=" when TryParseNumbers(paramValue, right, out var l, out var r):
                return l >= r;

            case "<" when TryParseNumbers(paramValue, right, out var l, out var r):
                return l < r;

            case "<=" when TryParseNumbers(paramValue, right, out var l, out var r):
                return l <= r;

            case "in":
                var items = right.Split(',', StringSplitOptions.TrimEntries);
                return items.Any(i => i.Equals(paramValue, StringComparison.OrdinalIgnoreCase));

            case "contains":
                return paramValue.Contains(right, StringComparison.OrdinalIgnoreCase);

            default:
                return false;
        }
    }

    private static bool TryParseNumbers(string left, string right, out decimal l, out decimal r)
    {
        l = r = 0;
        return decimal.TryParse(left, out l) && decimal.TryParse(right, out r);
    }

    [GeneratedRegex(
        @"\{#if\s+(?<condition>[^}]+)\}(?<true>(?:(?!\{#if\s)(?!\{/if\})(?!\{#else\}).)*)(?:\{#else\}(?<else>(?:(?!\{#if\s)(?!\{/if\}).)*)?)?\{/if\}",
        RegexOptions.Singleline)]
    private static partial Regex IfBlockRegex();
}
