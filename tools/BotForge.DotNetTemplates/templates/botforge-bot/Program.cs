using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BotForge.Core.Extensions;
using BotForge.Routing.Extensions;
using BotForge.Messaging;
using BotForge.Consumer.Extensions;
//#if (UseObservability)
using BotForge.Observability;
//#endif

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTelegramCore(options =>
{
    options.RegisterBot("main", builder.Configuration["BotToken"]
        ?? throw new InvalidOperationException("BotToken is required. Set it in appsettings.json or as an environment variable."));
});

builder.Services.AddTelegramRouting(options =>
{
    options.AddHandlersFromAssembly(typeof(Program).Assembly);
});

builder.Services.AddTelegramMessaging();

//#if (UseObservability)
var enablePrometheusExporter =
    bool.TryParse(builder.Configuration["Observability:EnablePrometheusExporter"], out var enabled)
        ? enabled
        : true;

var prometheusPrefixes = builder.Configuration
    .GetSection("Observability:PrometheusUriPrefixes")
    .GetChildren()
    .Select(child => child.Value)
    .Where(value => !string.IsNullOrWhiteSpace(value))
    .Cast<string>()
    .ToArray();

if (prometheusPrefixes.Length == 0)
    prometheusPrefixes = ["http://localhost:9464/"];

builder.Services.AddTelegramObservability(options =>
{
    options.EnablePrometheusExporter = enablePrometheusExporter;
    options.PrometheusUriPrefixes = prometheusPrefixes;
});
//#endif

//#if (ConsumerMode == "polling")
builder.Services.AddPollingConsumer();
//#else
// builder.Services.AddWebhookConsumer();
//#endif

var app = builder.Build();
await app.RunAsync();
