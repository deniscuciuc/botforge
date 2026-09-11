namespace TeleForge.Templates;

public interface ITemplateRenderer
{
    string Render(string templateName, IDictionary<string, string> parameters, string language);

    string Render(
        string templateName,
        IDictionary<string, string> parameters,
        string language,
        IDictionary<string, IReadOnlyList<IDictionary<string, string>>>? loopData);
}
