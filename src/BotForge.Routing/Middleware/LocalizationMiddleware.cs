using BotForge.Core;
using BotForge.Routing.Abstractions;
using Microsoft.Extensions.Logging;

namespace BotForge.Routing.Middleware;

/// <summary>
/// Middleware that resolves the user's locale and sets it on the update context.
/// </summary>
public class LocalizationMiddleware(IUserLocaleResolver resolver, ILogger<LocalizationMiddleware> logger)
    : ITelegramMiddleware
{
    public async Task InvokeAsync(TelegramUpdateContext context, TelegramUpdateDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.UserId is { } userId)
        {
            var telegramLang = context.RawUpdate.Message?.From?.LanguageCode
                               ?? context.RawUpdate.CallbackQuery?.From?.LanguageCode;

            var locale = await resolver.ResolveLocaleAsync(userId, telegramLang, context.CancellationToken).ConfigureAwait(false);
            context.Language = locale;

            logger.LogDebug("Resolved locale '{Locale}' for user {UserId}", locale, userId);
        }

        await next(context).ConfigureAwait(false);
    }
}
