using BotForge.Consumer.Extensions;
using BotForge.Messaging;
using BotForge.Messaging.MassTransit;
using BotForge.Messaging.Middleware;
using BotForge.Observability;
using BotForge.RateLimiting.Redis;
using BotForge.Routing.Extensions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

// --- Redis for distributed rate limiting ---
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(
        builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379"));

builder.Services.AddRedisRateLimitStore("advbot:ratelimit:");

// --- MassTransit + RabbitMQ for message queuing ---
builder.Services.AddMassTransit(bus =>
{
    bus.AddTelegramMessageConsumer();
    bus.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddTelegramMassTransitBackend();

// --- Optional Prometheus metrics export ---
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

// --- Multi-bot messaging with send middleware ---
builder.Services.AddTelegramMessaging(messaging =>
{
    messaging.AddBot("main", bot =>
    {
        bot.Token = builder.Configuration["Telegram:MainBotToken"]
                    ?? throw new InvalidOperationException("Set Telegram:MainBotToken");
    });

    messaging.AddBot("notifications", bot =>
    {
        bot.Token = builder.Configuration["Telegram:NotificationBotToken"]
                    ?? throw new InvalidOperationException("Set Telegram:NotificationBotToken");
    });

    // Send pipeline: rate limit → retry → circuit breaker → metrics
    messaging.UseSendMiddleware<RateLimitSendMiddleware>();
    messaging.UseSendMiddleware<RetrySendMiddleware>();
    messaging.UseSendMiddleware<CircuitBreakerSendMiddleware>();
    messaging.UseSendMiddleware<MetricsSendMiddleware>();
});

// --- Routing with authorization policies ---
builder.Services.AddTelegramRouting(routing =>
{
    routing.AddHandlersFromAssembly(typeof(Program).Assembly);

    routing.ConfigureAuthorization(auth =>
    {
        auth.AddPolicy("admin", policy =>
            policy.RequirePermission("admin"));

        auth.AddPolicy("private-only", policy =>
            policy.RequireChatType("Private"));

        auth.AddPolicy("moderator", policy =>
            policy.RequireRank("moderator", "admin"));
    });
});

// --- Consumer ---
builder.Services.AddTelegramConsumer(consumer =>
{
    consumer.ConcurrencyLimit = 100;
    consumer.EnableHealthChecks();
});

var app = builder.Build();
await app.RunAsync();
