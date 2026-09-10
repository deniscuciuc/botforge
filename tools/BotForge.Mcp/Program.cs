using BotForge.Mcp;
using BotForge.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options => { options.LogToStandardErrorThreshold = LogLevel.Trace; });

// Resolve workspace root from --workspace arg or current directory
var workspaceArg = args
    .SkipWhile(a => a != "--workspace")
    .Skip(1)
    .FirstOrDefault();

var workspaceRoot = workspaceArg ?? Directory.GetCurrentDirectory();
var workspace = new WorkspaceContext(workspaceRoot);

builder.Services.AddSingleton(workspace);
builder.Services.AddSingleton<RoslynAnalysisService>();
builder.Services.AddSingleton<TemplateValidationService>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly();

await builder.Build().RunAsync();
