using BotForge.Core;
using BotForge.Payments.Abstractions;
using BotForge.Payments.Router;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace BotForge.Payments.Checkout;

/// <summary>
/// Runs the chain of <see cref="IPreCheckoutValidator"/> instances registered for the payload prefix.
/// Answers the Telegram pre-checkout query with the result.
/// </summary>
public class PreCheckoutPipeline(
    ITelegramBotClientProvider botProvider,
    IServiceProvider services,
    IPaymentMetrics metrics,
    ILogger<PreCheckoutPipeline> logger)
{
    public async Task HandleAsync(PreCheckoutContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var client = botProvider.GetClient(context.BotId);

        try
        {
            var globalValidators = services.GetServices<GlobalPreCheckoutValidator>();
            foreach (var validator in globalValidators)
            {
                var result = await validator.ValidateAsync(context, ct).ConfigureAwait(false);
                if (result.Approved) continue;

                logger.LogInformation(
                    "Pre-checkout rejected by global validator {Validator} for user {UserId}: {Reason}",
                    validator.GetType().Name, context.UserId, result.ErrorMessage ?? result.ErrorTemplateKey);

                await client.AnswerPreCheckoutQuery(context.PreCheckoutQueryId,
                    result.ErrorMessage ?? result.ErrorTemplateKey ?? "Validation failed", ct).ConfigureAwait(false);

                metrics.CheckoutCompleted(context.BotId, context.PayloadPrefix, false);
                return;
            }

            var registry = services.GetRequiredService<PaymentHandlerRegistry>();
            var preCheckoutValidators = registry.GetValidators(context.PayloadPrefix, services);

            foreach (var validator in preCheckoutValidators)
            {
                var result = await validator.ValidateAsync(context, ct).ConfigureAwait(false);
                if (result.Approved) continue;

                logger.LogInformation(
                    "Pre-checkout rejected by {Validator} for user {UserId}, prefix {Prefix}: {Reason}",
                    validator.GetType().Name, context.UserId, context.PayloadPrefix,
                    result.ErrorMessage ?? result.ErrorTemplateKey);

                await client.AnswerPreCheckoutQuery(context.PreCheckoutQueryId,
                    result.ErrorMessage ?? result.ErrorTemplateKey ?? "Validation failed", ct).ConfigureAwait(false);

                metrics.CheckoutCompleted(context.BotId, context.PayloadPrefix, false);
                return;
            }

            await client.AnswerPreCheckoutQuery(context.PreCheckoutQueryId, cancellationToken: ct).ConfigureAwait(false);
            metrics.CheckoutCompleted(context.BotId, context.PayloadPrefix, true);
            logger.LogDebug("Pre-checkout approved for user {UserId}, prefix {Prefix}", context.UserId,
                context.PayloadPrefix);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pre-checkout pipeline failed for user {UserId}, prefix {Prefix}", context.UserId,
                context.PayloadPrefix);

            try
            {
                await client.AnswerPreCheckoutQuery(context.PreCheckoutQueryId, "Internal error", ct).ConfigureAwait(false);
            }
            catch (Exception innerEx)
            {
                logger.LogError(innerEx, "Failed to answer pre-checkout query after error");
            }

            metrics.CheckoutCompleted(context.BotId, context.PayloadPrefix, false);
        }
    }
}

/// <summary>
/// Marker base for global pre-checkout validators (runs for all payload types).
/// </summary>
public abstract class GlobalPreCheckoutValidator : IPreCheckoutValidator
{
    public abstract Task<CheckoutValidationResult> ValidateAsync(PreCheckoutContext context,
        CancellationToken ct = default);
}
