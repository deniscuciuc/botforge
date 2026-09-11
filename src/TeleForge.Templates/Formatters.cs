using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TeleForge.Templates;

public interface ITemplateFormatter
{
    string Name { get; }
    string Format(string value, string? args, CultureInfo culture);
}

public partial class FormatterPipeline
{
    private readonly Dictionary<string, ITemplateFormatter> _formatters;

    public FormatterPipeline(IEnumerable<ITemplateFormatter> formatters)
    {
        ArgumentNullException.ThrowIfNull(formatters);

        _formatters = new Dictionary<string, ITemplateFormatter>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in formatters)
            _formatters[f.Name] = f;

        // Register built-in formatters
        RegisterBuiltin(new NumberFormatter());
        RegisterBuiltin(new PercentFormatter());
        RegisterBuiltin(new TruncateFormatter());
        RegisterBuiltin(new UpperFormatter());
        RegisterBuiltin(new LowerFormatter());
        RegisterBuiltin(new DateFormatter());
        RegisterBuiltin(new DurationFormatter());
        RegisterBuiltin(new ProgressBarFormatter());
        RegisterBuiltin(new PluralFormatter());
    }

    private void RegisterBuiltin(ITemplateFormatter formatter)
    {
        _formatters.TryAdd(formatter.Name, formatter);
    }

    public string Process(string text, IDictionary<string, string> parameters, CultureInfo? culture = null)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('|'))
            return text;

        culture ??= CultureInfo.InvariantCulture;
        return PipeRegex().Replace(text, match =>
        {
            var paramName = match.Groups["param"].Value.Trim();
            var formatterStr = match.Groups["formatter"].Value.Trim();

            if (!parameters.TryGetValue(paramName, out var value))
                return match.Value;

            // Parse formatter:args
            var colonIndex = formatterStr.IndexOf(':');
            var formatterName = colonIndex >= 0 ? formatterStr[..colonIndex] : formatterStr;
            var args = colonIndex >= 0 ? formatterStr[(colonIndex + 1)..] : null;

            if (_formatters.TryGetValue(formatterName, out var formatter))
                return formatter.Format(value, args, culture);

            return match.Value;
        });
    }

    [GeneratedRegex(@"\{(?<param>[A-Za-z_][A-Za-z0-9_]*)\s*\|\s*(?<formatter>[^}]+)\}")]
    private static partial Regex PipeRegex();
}

public class NumberFormatter : ITemplateFormatter
{
    public string Name => "number";

    public string Format(string value, string? args, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (decimal.TryParse(value, CultureInfo.InvariantCulture, out var num))
            return num.ToString(string.IsNullOrEmpty(args) ? "N0" : args, culture);
        return value;
    }
}

public class PercentFormatter : ITemplateFormatter
{
    public string Name => "percent";

    public string Format(string value, string? args, CultureInfo culture)
    {
        if (decimal.TryParse(value, CultureInfo.InvariantCulture, out var num))
            return num.ToString(string.IsNullOrEmpty(args) ? "P1" : args, culture);
        return value;
    }
}

public class TruncateFormatter : ITemplateFormatter
{
    public string Name => "truncate";

    public string Format(string value, string? args, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (int.TryParse(args, out var maxLen) && value.Length > maxLen)
            return string.Concat(value.AsSpan(0, maxLen - 3), "...");
        return value;
    }
}

public class UpperFormatter : ITemplateFormatter
{
    public string Name => "upper";

    public string Format(string value, string? args, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value.ToUpper(culture);
    }
}

public class LowerFormatter : ITemplateFormatter
{
    public string Name => "lower";

    public string Format(string value, string? args, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value.ToLower(culture);
    }
}

public class DateFormatter : ITemplateFormatter
{
    public string Name => "date";

    public string Format(string value, string? args, CultureInfo culture)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, out var dto))
            return dto.ToString(string.IsNullOrEmpty(args) ? "dd.MM.yyyy" : args, culture);
        return value;
    }
}

public class DurationFormatter : ITemplateFormatter
{
    public string Name => "duration";

    public string Format(string value, string? args, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!TimeSpan.TryParse(value, out var ts) &&
            double.TryParse(value, CultureInfo.InvariantCulture, out var seconds))
            ts = TimeSpan.FromSeconds(seconds);

        if (ts == default)
            return value;

        var sb = new StringBuilder();
        if (ts.Days > 0) sb.Append(CultureInfo.InvariantCulture, $"{ts.Days}д ");
        if (ts.Hours > 0) sb.Append(CultureInfo.InvariantCulture, $"{ts.Hours}ч ");
        if (ts.Minutes > 0) sb.Append(CultureInfo.InvariantCulture, $"{ts.Minutes}м");
        if (sb.Length == 0) sb.Append(CultureInfo.InvariantCulture, $"{ts.Seconds}с");

        return sb.ToString().TrimEnd();
    }
}

public class ProgressBarFormatter : ITemplateFormatter
{
    public string Name => "progressbar";

    public string Format(string value, string? args, CultureInfo culture)
    {
        if (!decimal.TryParse(value, CultureInfo.InvariantCulture, out var ratio))
            return value;

        var width = 10;
        if (!string.IsNullOrEmpty(args) && int.TryParse(args, out var w))
            width = w;

        ratio = Math.Clamp(ratio, 0, 1);
        var filled = (int)Math.Round(ratio * width);
        var empty = width - filled;

        return $"[{new string('█', filled)}{new string('░', empty)}]";
    }
}

public class PluralFormatter : ITemplateFormatter
{
    public string Name => "plural";

    public string Format(string value, string? args, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(args) || !long.TryParse(value, out var number))
            return value;

        var forms = args.Split(',', StringSplitOptions.TrimEntries);
        if (forms.Length < 3)
            return value;

        // Russian pluralization rules
        var absNumber = Math.Abs(number);
        var mod10 = absNumber % 10;
        var mod100 = absNumber % 100;

        var formIndex = mod10 == 1 && mod100 != 11 ? 0 :
            mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20) ? 1 : 2;

        return $"{number} {forms[formIndex]}";
    }
}
