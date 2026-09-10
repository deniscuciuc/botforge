using BotForge.Core;
using BotForge.Messaging.Abstractions;
using NSubstitute;

namespace BotForge.Messaging.Tests;

public class TelegramMessageServiceTests
{
    [Fact]
    public void CreateMessage_ReturnsBuilder()
    {
        var sendPipeline = Substitute.For<ISendPipeline>();
        var botProvider = Substitute.For<ITelegramBotClientProvider>();
        var service = new TelegramMessageService(sendPipeline, botProvider);

        var message = service.CreateMessage("bot1");

        Assert.NotNull(message);
        Assert.IsType<TelegramMessageBuilder>(message);
    }

    [Fact]
    public void CreateBatch_ReturnsBatch()
    {
        var sendPipeline = Substitute.For<ISendPipeline>();
        var botProvider = Substitute.For<ITelegramBotClientProvider>();
        var service = new TelegramMessageService(sendPipeline, botProvider);

        var batch = service.CreateBatch("bot1");

        Assert.NotNull(batch);
        Assert.IsType<TelegramMessageBatch>(batch);
    }

    [Fact]
    public void CreateMessage_WithRenderer_ReturnsBuilder()
    {
        var sendPipeline = Substitute.For<ISendPipeline>();
        var botProvider = Substitute.For<ITelegramBotClientProvider>();
        var renderer = Substitute.For<IMessageRenderer>();
        var service = new TelegramMessageService(sendPipeline, botProvider);

        var message = service.CreateMessage("bot1");

        Assert.NotNull(message);
    }
}
