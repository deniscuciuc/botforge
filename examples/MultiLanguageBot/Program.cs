using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TeleForge.Consumer.Extensions;
using TeleForge.Messaging;
using TeleForge.Routing.Abstractions;
using TeleForge.Routing.Extensions;
using TeleForge.Templates;

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

builder.Services.AddTelegramTemplates();

// Register a simple in-memory locale store
builder.Services.AddSingleton<MultiLanguageBot.UserLocaleStore>();
builder.Services.AddSingleton<IUserLocaleResolver>(sp => sp.GetRequiredService<MultiLanguageBot.UserLocaleStore>());

builder.Services.AddTelegramConsumer(consumer => { consumer.ConcurrencyLimit = 20; });

var app = builder.Build();

// Load translations
var templateStore = app.Services.GetRequiredService<IMessageTemplateStore>();
templateStore.AddOrReplace(new MessageTemplate
{
    Name = "welcome",
    Text = new MessageTemplateText
    {
        Translations = new Dictionary<string, string>
        {
            ["en"] = "👋 <b>Welcome!</b>\nI support multiple languages.\nUse /lang to change language.",
            ["ru"] = "👋 <b>Добро пожаловать!</b>\nЯ поддерживаю несколько языков.\nИспользуйте /lang для смены языка.",
            ["uk"] = "👋 <b>Ласкаво просимо!</b>\nЯ підтримую кілька мов.\nВикористовуйте /lang для зміни мови."
        }
    }
});

templateStore.AddOrReplace(new MessageTemplate
{
    Name = "lang_changed",
    Text = new MessageTemplateText
    {
        Translations = new Dictionary<string, string>
        {
            ["en"] = "✅ Language set to <b>English</b>",
            ["ru"] = "✅ Язык установлен: <b>Русский</b>",
            ["uk"] = "✅ Мову встановлено: <b>Українська</b>"
        }
    }
});

templateStore.AddOrReplace(new MessageTemplate
{
    Name = "choose_lang",
    Text = new MessageTemplateText
    {
        Translations = new Dictionary<string, string>
        {
            ["en"] = "🌍 Choose your language:",
            ["ru"] = "🌍 Выберите язык:",
            ["uk"] = "🌍 Оберіть мову:"
        }
    }
});

await app.RunAsync();

namespace MultiLanguageBot
{
    public class UserLocaleStore : IUserLocaleResolver
    {
        private readonly ConcurrentDictionary<long, string> _locales = new();

        public Task<string> ResolveLocaleAsync(long userId, string? telegramLanguageCode,
            CancellationToken ct = default)
        {
            if (_locales.TryGetValue(userId, out var locale))
                return Task.FromResult(locale);
            return Task.FromResult(telegramLanguageCode ?? "en");
        }

        public void SetLocale(long userId, string locale)
        {
            _locales[userId] = locale;
        }
    }
}
