using BotForge.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddTelegramIngressHost(builder.Configuration);

var app = builder.Build();

app.MapTelegramWebhookEndpoints();
app.MapTelegramHealthEndpoints();

await app.RunAsync().ConfigureAwait(false);
