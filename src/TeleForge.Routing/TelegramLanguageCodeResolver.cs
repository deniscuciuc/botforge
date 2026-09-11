using TeleForge.Routing.Abstractions;

namespace TeleForge.Routing;

/// <summary>
/// Uses the Telegram LanguageCode from the user's Telegram client settings.
/// Fallback to a default locale if the code is null or empty.
/// </summary>
public class TelegramLanguageCodeResolver(string defaultLocale = "en") : IUserLocaleResolver
{
    public Task<string> ResolveLocaleAsync(long userId, string? telegramLanguageCode, CancellationToken ct = default)
    {
        var locale = string.IsNullOrEmpty(telegramLanguageCode) ? defaultLocale : telegramLanguageCode;
        return Task.FromResult(locale);
    }
}
