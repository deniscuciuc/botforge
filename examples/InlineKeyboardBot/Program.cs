using BotForge.Consumer.Extensions;
using BotForge.Messaging;
using BotForge.Routing.Extensions;
using BotForge.Templates;
using Microsoft.Extensions.DependencyInjection;
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

builder.Services.AddTelegramTemplates(options =>
{
    options.AddDirectory(Path.Combine(AppContext.BaseDirectory, "Templates"));
});

builder.Services.AddTelegramConsumer(consumer => { consumer.ConcurrencyLimit = 20; });

var app = builder.Build();

var templateLoader = app.Services.GetRequiredService<YamlTemplateLoader>();
templateLoader.LoadFromDirectory(Path.Combine(AppContext.BaseDirectory, "Templates"));

await app.RunAsync();
