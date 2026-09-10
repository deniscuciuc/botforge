using BotForge.Consumer.Extensions;
using BotForge.Messaging;
using BotForge.Payments.Abstractions;
using BotForge.Payments.Crypto.Extensions;
using BotForge.Payments.Extensions;
using BotForge.Routing.Extensions;
using CryptoShop.Data;
using CryptoShop.Handlers;
using CryptoShop.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<CryptoShopDbContext>(options =>
    options.UseSqlite("Data Source=cryptoshop.db"));

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
        handlers.AddValidator<CryptoShopCheckoutValidator>("crypto-shop");
        handlers.AddProcessor<CryptoShopStarsProcessor>("crypto-shop");
    });

// ── Cross-Rail Commerce ───────────────────────────────────────────────────────
//   AddPurchaseFulfillment wires PurchaseFulfillmentRegistry + Pipeline.
//   CryptoShopFulfillmentHandler handles Stars, TON Connect, and TON Direct rails.
builder.Services.AddPurchaseFulfillment(fulfillment =>
{
    fulfillment.AddFulfillmentHandler<CryptoShopFulfillmentHandler>("crypto-shop");
});

//   Wallet payment sessions: TON Connect + TON Direct rails
builder.Services.AddWalletPaymentSupport();

// ── TON Blockchain Observer + TON Service Factories ──────────────────────────
//   AddTonWalletPayments registers:
//     - TonConnectSessionService  (TON Connect / Mini App deep-link rail)
//     - TonDirectAddressService   (deposit-address + memo rail)
//     - TonBlockchainObserverWorker (background poller that calls IWalletPaymentService)
builder.Services.AddTonWalletPayments(ton =>
{
    ton.DepositAddress = builder.Configuration["TonPayments:DepositAddress"];
    ton.ApiKey = builder.Configuration["TonPayments:ApiKey"];
    ton.PollingInterval = TimeSpan.TryParse(
        builder.Configuration["TonPayments:PollingInterval"], out var interval)
        ? interval
        : TimeSpan.FromSeconds(15);
    ton.TonConnectDeepLinkTemplate = builder.Configuration["TonPayments:TonConnectDeepLinkTemplate"];
});

// ── Stores (application-owned persistence) ────────────────────────────────────
//   ICommerceStore      → PurchaseOrder + SettlementRecord
//   IWalletPaymentStore → WalletPaymentSession (required by WalletPaymentService & observer)
builder.Services.AddScoped<ICommerceStore, EfCommerceStore>();
builder.Services.AddScoped<IWalletPaymentStore, EfWalletPaymentStore>();

//   Register the fulfillment handler so PurchaseFulfillmentRegistry can resolve it
builder.Services.AddScoped<CryptoShopFulfillmentHandler>();

// ── Consumer ──────────────────────────────────────────────────────────────────
builder.Services.AddTelegramConsumer(consumer => { consumer.ConcurrencyLimit = 20; });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CryptoShopDbContext>();
    await db.Database.EnsureCreatedAsync();
}

await app.RunAsync();
