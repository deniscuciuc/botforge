using System.Text.Json;
using TeleForge.Consumer.Extensions;
using TeleForge.Consumer.Webhook;
using TeleForge.Core;
using TeleForge.Messaging;
using TeleForge.Routing.Extensions;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTelegramMessaging(messaging =>
{
    messaging.AddBot("main", bot =>
    {
        bot.Token = builder.Configuration["Telegram:BotToken"]
                    ?? throw new InvalidOperationException("Set Telegram:BotToken in appsettings.json");
        bot.Transport = UpdateTransport.Webhook;
    });
});

builder.Services.AddTelegramRouting(routing => { routing.AddHandlersFromAssembly(typeof(Program).Assembly); });

var webhookSecret = builder.Configuration["Telegram:WebhookSecret"] ?? "my-secret-token";

builder.Services.AddTelegramConsumer(consumer =>
{
    consumer.DefaultTransport = UpdateTransport.Webhook;
    consumer.ConcurrencyLimit = 50;
    consumer.ConfigureWebhook(webhook =>
    {
        webhook.Path = "/api/telegram/webhook/{botId}";
        webhook.SecretToken = webhookSecret;
        webhook.MaxConnections = 40;
        webhook.DropPendingUpdates = false;
    });
    consumer.EnableHealthChecks();
});

var app = builder.Build();

// Webhook endpoint — receives Telegram updates via HTTPS POST
app.MapPost("/api/telegram/webhook/{botId}", async (
    string botId,
    HttpRequest request,
    WebhookUpdateHandler handler,
    CancellationToken ct) =>
{
    var secretHeader = request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault();
    var update = await JsonSerializer.DeserializeAsync<Update>(
        request.Body,
        cancellationToken: ct);

    if (update is null)
        return Results.BadRequest();

    await handler.HandleAsync(botId, update, secretHeader, ct);
    return Results.Ok();
});

// Health check endpoint
app.MapGet("/health", (WebhookUpdateHandler _) => Results.Ok("Healthy"));

await app.RunAsync();
