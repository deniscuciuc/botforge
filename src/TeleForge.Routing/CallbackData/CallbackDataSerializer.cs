using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using TeleForge.Routing.Abstractions;

namespace TeleForge.Routing.CallbackData;

public class CallbackDataSerializer : ICallbackDataSerializer
{
    private static readonly ConcurrentDictionary<Type, (string Pattern, PropertyInfo[] Properties)> TypeCache = new();
    private static readonly Regex ParamRegex = new(@"\{(\w+)(?::(\w+))?\}", RegexOptions.Compiled);

    public string Serialize<T>(T data) where T : ICallbackData
    {
        var (pattern, properties) = GetTypeInfo(typeof(T));
        var result = pattern;

        foreach (var prop in properties)
        {
            var value = prop.GetValue(data)?.ToString() ?? string.Empty;
            // Replace both "{Name}" and "{Name:type}" placeholders
            result = Regex.Replace(result, @$"\{{{prop.Name}(?::(\w+))?\}}", value);
        }

        return result;
    }

    public T? Deserialize<T>(string callbackData) where T : ICallbackData
    {
        var result = Deserialize(callbackData, typeof(T));
        return result is T typed ? typed : default;
    }

    public object? Deserialize(string callbackData, Type type)
    {
        var (pattern, properties) = GetTypeInfo(type);

        if (!TryMatch(callbackData, pattern, out var parameters))
            return null;

        var instance = Activator.CreateInstance(type);
        if (instance is null)
            return null;

        foreach (var prop in properties)
        {
            if (!parameters.TryGetValue(prop.Name, out var value))
                continue;

            var converted = ConvertParameter(value, prop.PropertyType);
            if (converted != null)
                prop.SetValue(instance, converted);
        }

        return instance;
    }

    public bool TryMatch(string callbackData, string pattern, out IDictionary<string, string> parameters)
    {
        parameters = new Dictionary<string, string>();

        // Build regex from pattern: "shop:pack:{packId:int}" → "^shop:pack:(?<packId>.+)$"
        var regexPattern = "^" + ParamRegex.Replace(pattern, m =>
        {
            var name = m.Groups[1].Value;
            var typeConstraint = m.Groups[2].Value;
            var valuePattern = typeConstraint switch
            {
                "int" or "long" => @"-?\d+",
                "guid" => @"[0-9a-fA-F\-]{36}",
                "bool" => @"true|false",
                _ => @"[^:]+"
            };
            return $"(?<{name}>{valuePattern})";
        }) + "$";

        var match = Regex.Match(callbackData, regexPattern, RegexOptions.IgnoreCase);
        if (!match.Success)
            return false;

        foreach (var groupName in match.Groups.Keys)
        {
            if (groupName == "0") continue;
            parameters[groupName] = match.Groups[groupName].Value;
        }

        return true;
    }

    private static (string Pattern, PropertyInfo[] Properties) GetTypeInfo(Type type)
    {
        return TypeCache.GetOrAdd(type, t =>
        {
            var patternProp = t.GetProperty("Pattern",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            var pattern = patternProp?.GetValue(null) as string
                          ?? throw new InvalidOperationException($"Type {t.Name} must have a static Pattern property.");

            var paramNames = new HashSet<string>();
            foreach (Match m in ParamRegex.Matches(pattern))
                paramNames.Add(m.Groups[1].Value);

            var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => paramNames.Contains(p.Name))
                .ToArray();

            return (pattern, properties);
        });
    }

    private static object? ConvertParameter(string value, Type targetType)
    {
        if (targetType == typeof(string)) return value;
        if (targetType == typeof(int) && int.TryParse(value, out var i)) return i;
        if (targetType == typeof(long) && long.TryParse(value, out var l)) return l;
        if (targetType == typeof(bool) && bool.TryParse(value, out var b)) return b;
        if (targetType == typeof(Guid) && Guid.TryParse(value, out var g)) return g;
        return null;
    }
}
