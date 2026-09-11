namespace TeleForge.Templates;

public class TelegramTemplateOptions
{
    public List<string> Directories { get; set; } = ["Templates"];
    public string DefaultLanguage { get; set; } = "ru";
    public string FallbackLanguage { get; set; } = "en";
    public string? EmojiFile { get; set; }
    public bool HotReload { get; set; }
    public bool PreCompile { get; set; }

    internal List<Type> Formatters { get; } = [];

    public TelegramTemplateOptions AddFormatter<T>() where T : class, ITemplateFormatter
    {
        Formatters.Add(typeof(T));
        return this;
    }

    public TelegramTemplateOptions AddDirectory(string directory)
    {
        Directories.Add(directory);
        return this;
    }
}
