using BotForge.Consumer.Extensions;
using BotForge.Messaging;
using BotForge.Routing.Extensions;
using BotForge.Streaming;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTelegramMessaging(messaging =>
{
    messaging.AddBot("main", bot =>
    {
        bot.Token = builder.Configuration["Telegram:BotToken"]
                    ?? throw new InvalidOperationException("Set Telegram:BotToken in appsettings.json");
    });
});

builder.Services.AddTelegramRouting(routing => { routing.AddHandlersFromAssembly(typeof(Program).Assembly); });

builder.Services.AddTelegramConsumer(consumer => { consumer.ConcurrencyLimit = 10; });

// Register streaming services — BotId must match the key used in AddBot above.
builder.Services.AddTelegramStreaming(o => o.BotId = "main");

var app = builder.Build();
await app.RunAsync();
