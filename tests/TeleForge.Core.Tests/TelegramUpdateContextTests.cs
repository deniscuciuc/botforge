using TeleForge.TestUtilities;
using Telegram.Bot.Types;

namespace TeleForge.Core.Tests;

public class TelegramUpdateContextTests
{
    [Fact]
    public void Constructor_WithMessage_ResolvesCorrectly()
    {
        var update = UpdateFactory.CreateMessageUpdate(100, 200, "hello");
        var ctx = new TelegramUpdateContext(update, "bot1");

        Assert.Equal("bot1", ctx.BotId);
        Assert.Equal("Message", ctx.UpdateType);
        Assert.Equal(100L, ctx.ChatId);
        Assert.Equal(200L, ctx.UserId);
        Assert.Same(update, ctx.RawUpdate);
    }

    [Fact]
    public void Constructor_WithCallbackQuery_ResolvesCorrectly()
    {
        var update = UpdateFactory.CreateCallbackQueryUpdate(300, 400, "test:1");
        var ctx = new TelegramUpdateContext(update, "bot2");

        Assert.Equal("CallbackQuery", ctx.UpdateType);
        Assert.Equal(300L, ctx.ChatId);
        Assert.Equal(400L, ctx.UserId);
    }

    [Fact]
    public void Constructor_WithInlineQuery_ResolvesCorrectly()
    {
        var update = UpdateFactory.CreateInlineQueryUpdate(500, "search");
        var ctx = new TelegramUpdateContext(update, "bot3");

        Assert.Equal("InlineQuery", ctx.UpdateType);
        Assert.Null(ctx.ChatId);
        Assert.Equal(500L, ctx.UserId);
    }

    [Fact]
    public void Constructor_WithEmptyUpdate_ResolvesUnknown()
    {
        var update = new Update { Id = 1 };
        var ctx = new TelegramUpdateContext(update, "bot");

        Assert.Equal("Unknown", ctx.UpdateType);
        Assert.Null(ctx.ChatId);
        Assert.Null(ctx.UserId);
    }

    [Fact]
    public void SetFeature_And_GetFeature_RoundTrips()
    {
        var ctx = UpdateFactory.CreateContext(
            UpdateFactory.CreateMessageUpdate(1, 1, "test"));

        var feature = new TestFeature { Value = 42 };
        ctx.SetFeature(feature);

        var retrieved = ctx.GetFeature<TestFeature>();
        Assert.Equal(42, retrieved.Value);
    }

    [Fact]
    public void GetFeature_WhenMissing_Throws()
    {
        var ctx = UpdateFactory.CreateContext(
            UpdateFactory.CreateMessageUpdate(1, 1, "test"));

        Assert.Throws<InvalidOperationException>(() => ctx.GetFeature<TestFeature>());
    }

    [Fact]
    public void GetFeatureOrDefault_WhenMissing_ReturnsNull()
    {
        var ctx = UpdateFactory.CreateContext(
            UpdateFactory.CreateMessageUpdate(1, 1, "test"));

        Assert.Null(ctx.GetFeatureOrDefault<TestFeature>());
    }

    [Fact]
    public void Items_AreMutable()
    {
        var ctx = UpdateFactory.CreateContext(
            UpdateFactory.CreateMessageUpdate(1, 1, "test"));

        ctx.Items["key"] = "value";
        Assert.Equal("value", ctx.Items["key"]);
    }

    [Fact]
    public void Language_And_UserRank_AreSettable()
    {
        var ctx = UpdateFactory.CreateContext(
            UpdateFactory.CreateMessageUpdate(1, 1, "test"));

        ctx.Language = "en";
        ctx.UserRank = "admin";

        Assert.Equal("en", ctx.Language);
        Assert.Equal("admin", ctx.UserRank);
    }

    private sealed class TestFeature
    {
        public int Value { get; init; }
    }
}
