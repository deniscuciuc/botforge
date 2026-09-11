using TeleForge.Routing.Pipeline;
using TeleForge.TestUtilities;

namespace TeleForge.Routing.Tests;

public class MiddlewarePipelineBuilderTests
{
    [Fact]
    public async Task Build_WithNoMiddleware_ExecutesTerminal()
    {
        var builder = new MiddlewarePipelineBuilder();
        var executed = false;

        var pipeline = builder.Build(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        var ctx = UpdateFactory.CreateContext(UpdateFactory.CreateMessageUpdate(1, 1, "test"));
        await pipeline(ctx);

        Assert.True(executed);
    }

    [Fact]
    public async Task Middleware_ExecutesInOrder()
    {
        var order = new List<int>();
        var builder = new MiddlewarePipelineBuilder();

        builder.Use(next => async ctx =>
        {
            order.Add(1);
            await next(ctx);
            order.Add(4);
        });

        builder.Use(next => async ctx =>
        {
            order.Add(2);
            await next(ctx);
            order.Add(3);
        });

        var pipeline = builder.Build(_ =>
        {
            order.Add(0);
            return Task.CompletedTask;
        });

        var ctx = UpdateFactory.CreateContext(UpdateFactory.CreateMessageUpdate(1, 1, "test"));
        await pipeline(ctx);

        Assert.Equal([1, 2, 0, 3, 4], order);
    }

    [Fact]
    public async Task Middleware_CanShortCircuit()
    {
        var builder = new MiddlewarePipelineBuilder();
        var terminalCalled = false;

        builder.Use(next => ctx =>
        {
            // Don't call next — short-circuit
            return Task.CompletedTask;
        });

        var pipeline = builder.Build(_ =>
        {
            terminalCalled = true;
            return Task.CompletedTask;
        });

        var ctx = UpdateFactory.CreateContext(UpdateFactory.CreateMessageUpdate(1, 1, "test"));
        await pipeline(ctx);

        Assert.False(terminalCalled);
    }
}
