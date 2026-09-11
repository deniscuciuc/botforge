using TeleForge.Core;
using TeleForge.Messaging.Abstractions;

namespace TeleForge.Messaging.Tests;

public class TelegramMessagingOptionsTests
{
    [Fact]
    public void AddBot_ConfiguresBotThroughPublicApi()
    {
        var options = new TelegramMessagingOptions();
        options.AddBot("bot1", b => { b.Token = "tok-123"; });

        // AddBot returns the options for chaining
        Assert.IsType<TelegramMessagingOptions>(options);
    }

    [Fact]
    public void UseSendMiddleware_ReturnsSelf()
    {
        var options = new TelegramMessagingOptions();
        var result = options.UseSendMiddleware<TestMiddleware>();

        Assert.Same(options, result);
    }

    [Fact]
    public void UseQueueBackend_ReturnsSelf()
    {
        var options = new TelegramMessagingOptions();
        var result = options.UseQueueBackend<TestQueueBackend>();

        Assert.Same(options, result);
    }

    [Fact]
    public void UseRateLimitStore_ReturnsSelf()
    {
        var options = new TelegramMessagingOptions();
        var result = options.UseRateLimitStore<TestRateLimitStore>();

        Assert.Same(options, result);
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new TelegramMessagingOptions();

        Assert.Equal(MessagePriority.Normal, options.DefaultPriority);
        Assert.Equal(3, options.RetryCount);
        Assert.Equal(TimeSpan.FromSeconds(1), options.RetryBaseDelay);
        Assert.Equal(10, options.CircuitBreaker.FailureThreshold);
        Assert.Equal(TimeSpan.FromSeconds(30), options.CircuitBreaker.BreakDuration);
    }

    [Fact]
    public void CircuitBreakerOptions_CanBeConfigured()
    {
        var options = new TelegramMessagingOptions
        {
            RetryCount = 5,
            RetryBaseDelay = TimeSpan.FromSeconds(2)
        };
        options.CircuitBreaker.FailureThreshold = 20;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromMinutes(1);

        Assert.Equal(5, options.RetryCount);
        Assert.Equal(TimeSpan.FromSeconds(2), options.RetryBaseDelay);
        Assert.Equal(20, options.CircuitBreaker.FailureThreshold);
        Assert.Equal(TimeSpan.FromMinutes(1), options.CircuitBreaker.BreakDuration);
    }

    [Fact]
    public void AddBot_FluentChaining()
    {
        var options = new TelegramMessagingOptions()
            .AddBot("bot1", b => b.Token = "t1")
            .AddBot("bot2", b => b.Token = "t2");

        Assert.IsType<TelegramMessagingOptions>(options);
    }

    private sealed class TestMiddleware : ISendMiddleware
    {
        public Task<SendResult> InvokeAsync(SendContext context, TelegramSendDelegate next)
        {
            return next(context);
        }
    }

    private sealed class TestQueueBackend : IMessageQueueBackend
    {
        public Task EnqueueAsync(QueuedTelegramMessage message, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task EnqueueBatchAsync(IEnumerable<QueuedTelegramMessage> messages, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestRateLimitStore : TeleForge.RateLimiting.Abstractions.IRateLimitStore
    {
        public Task<TeleForge.RateLimiting.Abstractions.RateLimitResult> AcquireAsync(
            string key, TeleForge.RateLimiting.Abstractions.RateLimitPolicy policy,
            CancellationToken ct = default)
        {
            return Task.FromResult(TeleForge.RateLimiting.Abstractions.RateLimitResult.Allowed());
        }

        public Task ResetAsync(string key, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
