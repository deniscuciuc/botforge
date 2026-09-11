using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using TeleForge.Core;
using TeleForge.Messaging.Abstractions;
using TeleForge.Messaging.Metrics;
using Telegram.Bot.Exceptions;

namespace TeleForge.Messaging.Middleware;

public class RetrySendMiddleware : ISendMiddleware
{
    private readonly ResiliencePipeline<SendResult> _pipeline;

    public RetrySendMiddleware(TelegramMessagingOptions options, ILogger<RetrySendMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _pipeline = new ResiliencePipelineBuilder<SendResult>()
            .AddRetry(new RetryStrategyOptions<SendResult>
            {
                MaxRetryAttempts = options.RetryCount,
                BackoffType = DelayBackoffType.Exponential,
                Delay = options.RetryBaseDelay,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<SendResult>()
                    .Handle<ApiRequestException>(ex =>
                        ex.ErrorCode is 429 or >= 500)
                    .HandleResult(r => r is { Success: false, RetryAfter: not null }),
                DelayGenerator = args =>
                {
                    if (args.Outcome.Result?.RetryAfter is not { } retryAfter)
                        return new ValueTask<TimeSpan?>((TimeSpan?)null);

                    // Cap the delay so we don't wait minutes inside a retry loop
                    var capped = retryAfter > options.MaxRetryDelay ? options.MaxRetryDelay : retryAfter;
                    return new ValueTask<TimeSpan?>(capped);
                },
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Retry attempt {Attempt}/{Max} for message to chat {ChatId}. Delay: {Delay}",
                        args.AttemptNumber + 1,
                        options.RetryCount,
                        args.Outcome.Result?.ChatId,
                        args.RetryDelay);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<SendResult> InvokeAsync(SendContext context, TelegramSendDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await _pipeline.ExecuteAsync(async ct =>
            {
                context.CancellationToken = ct;
                context.Attempt++;
                return await next(context).ConfigureAwait(false);
            }, context.CancellationToken).ConfigureAwait(false);
        }
        finally
        {
            var retries = Math.Max(0, context.Attempt - 1);
            MessagingMetrics.RecordRetryAttempts(context.Message.BotId, retries);
        }
    }
}
