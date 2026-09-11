using TeleForge.Routing.Handlers;
using TeleForge.TestUtilities;

namespace TeleForge.Routing.Tests;

public class CommandContextTests
{
    [Fact]
    public void Properties_AreResolved()
    {
        var updateCtx = UpdateFactory.CreateCommandContext(
            100, 200, "/start", "deep_link");

        var ctx = new CommandContext(updateCtx, "/start", ["deep_link"], "/start deep_link");

        Assert.Equal("/start", ctx.Command);
        Assert.Equal("deep_link", ctx.Arguments[0]);
        Assert.Equal("/start deep_link", ctx.RawText);
        Assert.Equal(100L, ctx.ChatId);
        Assert.Equal(200L, ctx.UserId);
    }

    [Fact]
    public void CancellationToken_DelegatesFromUpdateContext()
    {
        var cts = new CancellationTokenSource();
        var updateCtx = UpdateFactory.CreateCommandContext(1, 1, "/test");
        updateCtx.CancellationToken = cts.Token;

        var ctx = new CommandContext(updateCtx, "/test", [], "/test");

        Assert.Equal(cts.Token, ctx.CancellationToken);
    }
}

public class CallbackContextTests
{
    [Fact]
    public void Properties_AreResolved()
    {
        var updateCtx = UpdateFactory.CreateCallbackContext(
            300, 400, "shop:item:5");

        var ctx = new CallbackContext(
            updateCtx,
            "shop:item:5",
            updateCtx.RawUpdate.CallbackQuery!.Id,
            new Dictionary<string, string> { ["id"] = "5" });

        Assert.Equal("shop:item:5", ctx.CallbackData);
        Assert.Equal(300L, ctx.ChatId);
        Assert.Equal(400L, ctx.UserId);
        Assert.Equal("5", ctx.GetRouteParam("id"));
    }
}

public class HandlerResultsTests
{
    [Fact]
    public void CommandResult_Ok()
    {
        var result = CommandResult.Ok();
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void CommandResult_Fail()
    {
        var result = CommandResult.Fail("something broke");
        Assert.False(result.Success);
        Assert.Equal("something broke", result.ErrorMessage);
    }

    [Fact]
    public void CallbackResult_Ok()
    {
        var result = CallbackResult.Ok();
        Assert.True(result.Success);
    }

    [Fact]
    public void CallbackResult_Alert()
    {
        var result = CallbackResult.Alert("Done!");
        Assert.True(result.Success);
        Assert.Equal("Done!", result.AlertText);
        Assert.True(result.ShowAlert);
    }

    [Fact]
    public void CallbackResult_Fail()
    {
        var result = CallbackResult.Fail("error");
        Assert.False(result.Success);
        Assert.Equal("error", result.ErrorMessage);
    }
}
