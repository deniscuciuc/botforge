using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TeleForge.Consumer.Extensions;
using TeleForge.Messaging;
using TeleForge.Payments.Extensions;
using TeleForge.Routing.Extensions;
using TelegramShop.Data;
using TelegramShop.Handlers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ShopDbContext>(options =>
    options.UseSqlite("Data Source=shop.db"));

builder.Services.AddTelegramMessaging(messaging =>
{
    messaging.AddBot("main", bot =>
    {
        bot.Token = builder.Configuration["Telegram:BotToken"]
                    ?? throw new InvalidOperationException("Set Telegram:BotToken in appsettings.json");
    });
});

builder.Services.AddTelegramRouting(routing => { routing.AddHandlersFromAssembly(typeof(Program).Assembly); });

builder.Services.AddTelegramPayments(
    options => { options.AutoRefundOnFailure = true; },
    handlers =>
    {
        handlers.AddValidator<ShopCheckoutValidator>("shop");
        handlers.AddProcessor<ShopPaymentProcessor>("shop");
    });

builder.Services.AddTelegramConsumer(consumer => { consumer.ConcurrencyLimit = 20; });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
    await db.Database.EnsureCreatedAsync();
}

await app.RunAsync();
