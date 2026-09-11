namespace TeleForge.Templates;

public interface ILocalizationKeyResolver
{
    string ResolveText(string? localizationKey, string templateText, string language,
        IDictionary<string, string> parameters);

    string ResolveButtonText(string? localizationKey, string templateText, string language);
}
