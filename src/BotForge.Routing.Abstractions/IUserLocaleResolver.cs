namespace BotForge.Routing.Abstractions;

/// <summary>
/// Resolves the locale/language for a user.
/// </summary>
public interface IUserLocaleResolver
{
    Task<string> ResolveLocaleAsync(long userId, string? telegramLanguageCode, CancellationToken ct = default);
}
