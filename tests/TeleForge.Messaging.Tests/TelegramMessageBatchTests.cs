using NSubstitute;
using TeleForge.Core;
using TeleForge.Messaging.Abstractions;

namespace TeleForge.Messaging.Tests;

public class TelegramMessageBatchTests
{
    private readonly ISendPipeline _sendPipeline = Substitute.For<ISendPipeline>();
    private readonly IMessageQueueBackend _queueBackend = Substitute.For<IMessageQueueBackend>();
    private readonly ITelegramBotClientProvider _botProvider = Substitute.For<ITelegramBotClientProvider>();
    private readonly ITelegramMessageService _messageService = Substitute.For<ITelegramMessageService>();

    public TelegramMessageBatchTests()
    {
        _botProvider.GetConfiguration("test-bot")
            .Returns(new BotConfiguration { Key = "test-bot", Token = "tok" });
    }

    [Fact]
    public void Add_ReturnsSelf()
    {
        var batch = new TelegramMessageBatch("test-bot", _queueBackend, _sendPipeline, _botProvider);
        var message = Substitute.For<ITelegramMessage>();
        message.Build().Returns(new QueuedTelegramMessage { BotId = "test-bot", ChatId = 1 });

        var result = batch.Add(message);

        Assert.Same(batch, result);
    }

    [Fact]
    public async Task SendAsync_CallsSendPipelineForEachMessage()
    {
        _sendPipeline.SendAsync(Arg.Any<SendContext>(), Arg.Any<CancellationToken>())
            .Returns(SendResult.Ok(1, 1));

        var batch = new TelegramMessageBatch("test-bot", _queueBackend, _sendPipeline, _botProvider);

        var msg1 = Substitute.For<ITelegramMessage>();
        msg1.Build().Returns(new QueuedTelegramMessage { BotId = "test-bot", ChatId = 1 });
        var msg2 = Substitute.For<ITelegramMessage>();
        msg2.Build().Returns(new QueuedTelegramMessage { BotId = "test-bot", ChatId = 2 });

        batch.Add(msg1).Add(msg2);
        var results = await batch.SendAsync();

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
        await _sendPipeline.Received(2).SendAsync(Arg.Any<SendContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueAsync_EnqueuesAllMessages()
    {
        var batch = new TelegramMessageBatch("test-bot", _queueBackend, _sendPipeline, _botProvider);

        var msg1 = Substitute.For<ITelegramMessage>();
        msg1.Build().Returns(new QueuedTelegramMessage { BotId = "test-bot", ChatId = 1 });
        var msg2 = Substitute.For<ITelegramMessage>();
        msg2.Build().Returns(new QueuedTelegramMessage { BotId = "test-bot", ChatId = 2 });

        batch.Add(msg1).Add(msg2);
        var results = await batch.QueueAsync();

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
        await _queueBackend.Received(1)
            .EnqueueBatchAsync(Arg.Any<IEnumerable<QueuedTelegramMessage>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueAsync_WithoutBackend_FallsBackToSend()
    {
        _sendPipeline.SendAsync(Arg.Any<SendContext>(), Arg.Any<CancellationToken>())
            .Returns(SendResult.Ok(1, 1));

        var batch = new TelegramMessageBatch("test-bot", null, _sendPipeline, _botProvider);

        var msg = Substitute.For<ITelegramMessage>();
        msg.Build().Returns(new QueuedTelegramMessage { BotId = "test-bot", ChatId = 1 });

        batch.Add(msg);
        var results = await batch.QueueAsync();

        Assert.Single(results);
        Assert.True(results[0].Success);
        await _sendPipeline.Received(1).SendAsync(Arg.Any<SendContext>(), Arg.Any<CancellationToken>());
    }
}
