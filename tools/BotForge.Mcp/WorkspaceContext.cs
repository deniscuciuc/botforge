namespace BotForge.Mcp;

public sealed class WorkspaceContext
{
    public string RootPath { get; }
    public string DocsPath { get; }
    public string ExamplesPath { get; }
    public string SrcPath { get; }

    public WorkspaceContext(string rootPath)
    {
        RootPath = Path.GetFullPath(rootPath);
        DocsPath = Path.Combine(RootPath, "docs");
        ExamplesPath = Path.Combine(RootPath, "examples");
        SrcPath = Path.Combine(RootPath, "src");
    }

    public string? ReadDoc(string name)
    {
        var path = Path.Combine(DocsPath, $"{name}.md");
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    public string? ReadExampleFiles(string exampleName)
    {
        var dir = Path.Combine(ExamplesPath, exampleName);
        if (!Directory.Exists(dir))
            return null;

        var sb = new System.Text.StringBuilder();
        foreach (var file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(dir, file);
            sb.AppendLine($"// === {relative} ===");
            sb.AppendLine(File.ReadAllText(file));
            sb.AppendLine();
        }

        var programJson = Path.Combine(dir, "appsettings.json");
        if (File.Exists(programJson))
        {
            sb.AppendLine("// === appsettings.json ===");
            sb.AppendLine(File.ReadAllText(programJson));
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }
}
