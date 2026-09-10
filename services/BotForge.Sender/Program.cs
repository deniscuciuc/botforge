using BotForge.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddTelegramSenderHost(builder.Configuration);

var app = builder.Build();

app.MapTelegramHealthEndpoints();

await app.RunAsync().ConfigureAwait(false);
