using BotForge.Consumer.Extensions;
using BotForge.Messaging;
using BotForge.Payments.Abstractions;
using BotForge.Payments.Extensions;
using BotForge.Routing.Extensions;
using GiftCommerceBot.Data;
using GiftCommerceBot.Handlers;
using GiftCommerceBot.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<GiftShopDbContext>(options =>
    options.UseSqlite("Data Source=giftshop.db"));

// ── Telegram Messaging ────────────────────────────────────────────────────────
builder.Services.AddTelegramMessaging(messaging =>
{
    messaging.AddBot("main", bot =>
    {
        bot.Token = builder.Configuration["Telegram:BotToken"]
                    ?? throw new InvalidOperationException("Set Telegram:BotToken in appsettings.json");
    });
});

// ── Routing ───────────────────────────────────────────────────────────────────
builder.Services.AddTelegramRouting(routing => { routing.AddHandlersFromAssembly(typeof(Program).Assembly); });

// ── Base Payments (Stars invoices) ────────────────────────────────────────────
builder.Services.AddTelegramPayments(
    options => { options.AutoRefundOnFailure = true; },
    handlers =>
    {
        handlers.AddValidator<GiftShopCheckoutValidator>("gift-shop");
        handlers.AddProcessor<GiftShopStarsProcessor>("gift-shop");
    });

// ── Cross-Rail Commerce ───────────────────────────────────────────────────────
//   AddPurchaseFulfillment wires the PurchaseFulfillmentRegistry + Pipeline.
//   Register GiftShopFulfillmentHandler for the "gift-shop" prefix — this
//   applies to both GiftInbound and Stars rails once a SettlementRecord arrives.
builder.Services.AddPurchaseFulfillment(fulfillment =>
{
    fulfillment.AddFulfillmentHandler<GiftShopFulfillmentHandler>("gift-shop");
});

//   Inbound-gift-as-payment: users send a Telegram Gift to "pay" for an item.
builder.Services.AddGiftInboundSupport();

// ── Stores (application-owned persistence) ────────────────────────────────────
//   ICommerceStore  → tracks PurchaseOrder + SettlementRecord
//   IGiftIntakeStore → deduplicates received gifts
builder.Services.AddScoped<ICommerceStore, EfCommerceStore>();
builder.Services.AddScoped<IGiftIntakeStore, EfGiftIntakeStore>();

//   Register fulfillment handler as scoped so PurchaseFulfillmentRegistry can resolve it
builder.Services.AddScoped<GiftShopFulfillmentHandler>();

// ── Consumer ──────────────────────────────────────────────────────────────────
builder.Services.AddTelegramConsumer(consumer => { consumer.ConcurrencyLimit = 20; });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GiftShopDbContext>();
    await db.Database.EnsureCreatedAsync();
}

await app.RunAsync();
