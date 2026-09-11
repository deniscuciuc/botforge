using Microsoft.Extensions.Logging;
using NSubstitute;
using TeleForge.Core;
using TeleForge.Routing.Middleware;
using TeleForge.TestUtilities;

namespace TeleForge.Routing.Tests;

public class MetricsMiddlewareMetricsTests
{
    [Fact]
    public async Task InvokeAsync_EmitsRoutingUpdateMetric()
    {
        TelegramMetricsRuntime.Configure(false, true, false, false, false);
        using var capture = new MetricCaptureListener("TeleForge.Routing");
        var logger = Substitute.For<ILogger<MetricsMiddleware>>();
        var sut = new MetricsMiddleware(logger);

        var context = UpdateFactory.CreateContext(UpdateFactory.CreateMessageUpdate(1001, 2002, "test"), "main");

        await sut.InvokeAsync(context, ctx =>
        {
            ctx.Result = UpdateResult.Ok();
            return Task.CompletedTask;
        });

        var records = capture.ForInstrument("teleforge.telegram.routing.updates.total");
        var metric = Assert.Single(records);

        Assert.Equal(1d, metric.Value);
        Assert.Equal("main", metric.Tags["bot_key"]);
        Assert.Equal("success", metric.Tags["status"]);
    }
}
