using BotForge.Core;
using BotForge.Routing.Abstractions;
using BotForge.Routing.Attributes;
using BotForge.Routing.Registration;
using BotForge.Routing.Routing;
using Microsoft.Extensions.Logging;

namespace BotForge.Routing.Middleware;

public class AuthorizationMiddleware(
    IReadOnlyDictionary<string, IAuthorizationPolicy> policies,
    IHandlerRegistry registry,
    ILogger<AuthorizationMiddleware> logger)
    : ITelegramMiddleware
{
    public async Task InvokeAsync(TelegramUpdateContext context, TelegramUpdateDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var authorizeAttributes = ResolveAuthorizeAttributes(context);
        if (authorizeAttributes.Length == 0)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        foreach (var attr in authorizeAttributes)
        {
            if (!policies.TryGetValue(attr.Policy, out var policy))
            {
                logger.LogWarning("Authorization policy '{Policy}' not found", attr.Policy);
                context.Result = UpdateResult.Blocked($"Authorization policy '{attr.Policy}' not configured.");
                return;
            }

            var result = await policy.EvaluateAsync(context, context.CancellationToken).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                var failureReason = BuildFailureReason();
                logger.LogDebug(
                    "Authorization failed for user {UserId}: policy '{Policy}' — {Reason}",
                    context.UserId, attr.Policy, failureReason);
                context.Result = UpdateResult.Blocked(failureReason);
                return;
            }
        }

        await next(context).ConfigureAwait(false);
    }

    private AuthorizeAttribute[] ResolveAuthorizeAttributes(TelegramUpdateContext context)
    {
        var text = context.RawUpdate.Message?.Text;
        if (CommandTextParser.TryParse(text, out var cmd, out _))
        {
            var reg = registry.FindCommand(cmd, context.BotId);
            return reg?.AuthorizeAttributes ?? [];
        }

        var callbackData = context.RawUpdate.CallbackQuery?.Data;
        if (callbackData == null) return [];
        {
            var reg = registry.FindCallback(callbackData);
            return reg?.AuthorizeAttributes ?? [];
        }
    }

    private static string BuildFailureReason()
        => "You don't have permission to perform this action.";
}
