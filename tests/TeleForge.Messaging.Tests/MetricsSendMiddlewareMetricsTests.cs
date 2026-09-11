using Microsoft.Extensions.Logging;
using NSubstitute;
using TeleForge.Core;
using TeleForge.Messaging.Abstractions;
using TeleForge.Messaging.Middleware;
using TeleForge.TestUtilities;

namespace TeleForge.Messaging.Tests;

public class MetricsSendMiddlewareMetricsTests
{
    [Fact]
    public async Task InvokeAsync_EmitsSendRequestMetric()
    {
        TelegramMetricsRuntime.Configure(true, false, false, false, false);
        using var capture = new MetricCaptureListener("TeleForge.Messaging");
        var logger = Substitute.For<ILogger<MetricsSendMiddleware>>();
        var sut = new MetricsSendMiddleware(logger);

        var context = new SendContext
        {
            Message = new QueuedTelegramMessage
            {
                BotId = "main",
                ChatId = 1001,
                RawText = "hello"
            },
            Bot = new BotConfiguration
            {
                Key = "main",
                Token = "test-token"
            }
        };

        var result = await sut.InvokeAsync(context, _ => Task.FromResult(SendResult.Ok(context.Message.ChatId, 123)));

        Assert.True(result.Success);

        var records = capture.ForInstrument("teleforge.telegram.messaging.send.requests");
        var metric = Assert.Single(records);

        Assert.Equal(1d, metric.Value);
        Assert.Equal("main", metric.Tags["bot_key"]);
        Assert.Equal("success", metric.Tags["status"]);
    }
}
