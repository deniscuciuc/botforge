namespace TeleForge.Core.Tests;

public class SendResultTests
{
    [Fact]
    public void Ok_ReturnsSuccessWithIds()
    {
        var result = SendResult.Ok(100, 42);

        Assert.True(result.Success);
        Assert.Equal(100L, result.ChatId);
        Assert.Equal(42, result.MessageId);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failed_ReturnsFailure()
    {
        var result = SendResult.Failed("bad request", 400);

        Assert.False(result.Success);
        Assert.Equal("bad request", result.Error);
        Assert.Equal(400, result.ErrorCode);
    }

    [Fact]
    public void RateLimited_ReturnsRetryAfter()
    {
        var result = SendResult.RateLimited(TimeSpan.FromSeconds(5));

        Assert.False(result.Success);
        Assert.Equal(TimeSpan.FromSeconds(5), result.RetryAfter);
    }
}

public class UpdateResultTests
{
    [Fact]
    public void Ok_ReturnsSuccess()
    {
        var result = UpdateResult.Ok();
        Assert.True(result.Success);
    }

    [Fact]
    public void Blocked_ReturnsFailureWithReason()
    {
        var result = UpdateResult.Blocked("forbidden");
        Assert.False(result.Success);
        Assert.Equal("forbidden", result.Reason);
    }

    [Fact]
    public void RetryLater_SetsRetryAfter()
    {
        var result = UpdateResult.RetryLater(TimeSpan.FromSeconds(10));
        Assert.False(result.Success);
        Assert.Equal(TimeSpan.FromSeconds(10), result.RetryAfter);
    }
}
