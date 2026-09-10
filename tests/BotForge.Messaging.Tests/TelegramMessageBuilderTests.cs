using BotForge.Core;
using BotForge.Messaging.Abstractions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotForge.Messaging.Tests;

public class TelegramMessageBuilderTests
{
    private readonly ISendPipeline _sendPipeline = Substitute.For<ISendPipeline>();
    private readonly IMessageQueueBackend _queueBackend = Substitute.For<IMessageQueueBackend>();
    private readonly ITelegramBotClientProvider _botProvider = Substitute.For<ITelegramBotClientProvider>();

    private TelegramMessageBuilder CreateBuilder(string botId = "test-bot")
    {
        return new TelegramMessageBuilder(botId, _sendPipeline, _queueBackend, _botProvider);
    }

    [Fact]
    public void Build_SetsChatId()
    {
        var msg = CreateBuilder().ToChat(12345).WithText("hi").Build();
        Assert.Equal(12345, msg.ChatId);
    }

    [Fact]
    public void Build_SetsBotKey()
    {
        var msg = CreateBuilder("my-bot").ToChat(1).WithText("x").Build();
        Assert.Equal("my-bot", msg.BotId);
    }

    [Fact]
    public void Build_SetsRawText()
    {
        var msg = CreateBuilder().ToChat(1).WithText("Hello World").Build();
        Assert.Equal("Hello World", msg.RawText);
    }

    [Fact]
    public void Build_SetsHtmlParseMode()
    {
        var msg = CreateBuilder().ToChat(1).WithHtml("<b>bold</b>").Build();
        Assert.Equal(TelegramParseMode.Html, msg.ParseMode);
        Assert.Equal("<b>bold</b>", msg.RawText);
    }

    [Fact]
    public void Build_SetsTemplateKey()
    {
        var msg = CreateBuilder().ToChat(1).WithTemplate("welcome").Build();
        Assert.Equal("welcome", msg.TemplateKey);
    }

    [Fact]
    public void Build_SetsParameters()
    {
        var msg = CreateBuilder()
            .ToChat(1)
            .WithTemplate("t")
            .WithParameter("name", "Alice")
            .WithParameter("age", "30")
            .Build();

        Assert.Equal("Alice", msg.Parameters["name"]);
        Assert.Equal("30", msg.Parameters["age"]);
    }

    [Fact]
    public void Build_SetsParametersDictionary()
    {
        var parameters = new Dictionary<string, string> { ["key"] = "value" };
        var msg = CreateBuilder()
            .ToChat(1)
            .WithTemplate("t")
            .WithParameters(parameters)
            .Build();

        Assert.Equal("value", msg.Parameters["key"]);
    }

    [Fact]
    public void Build_SetsLanguage()
    {
        var msg = CreateBuilder().ToChat(1).WithText("x").WithLanguage("en").Build();
        Assert.Equal("en", msg.Language);
    }

    [Fact]
    public void Build_SetsThreadId()
    {
        var msg = CreateBuilder().ToChat(1).InThread(42).WithText("x").Build();
        Assert.Equal(42, msg.ThreadId);
    }

    [Fact]
    public void Build_SetsReplyToMessageId()
    {
        var msg = CreateBuilder().ToChat(1).ReplyToMessage(77).WithText("x").Build();
        Assert.Equal(77, msg.ReplyToMessageId);
    }

    [Fact]
    public void Build_SetsEditMessage()
    {
        var msg = CreateBuilder().ToChat(1).EditMessage(100).WithText("updated").Build();
        Assert.Equal(100, msg.EditMessageId);
        Assert.False(msg.EditKeyboardOnly);
    }

    [Fact]
    public void Build_SetsEditKeyboardOnly()
    {
        var msg = CreateBuilder().ToChat(1).EditKeyboardOnly(100).Build();
        Assert.Equal(100, msg.EditMessageId);
        Assert.True(msg.EditKeyboardOnly);
    }

    [Fact]
    public void Build_SetsPriority()
    {
        var msg = CreateBuilder().ToChat(1).WithText("x").WithPriority(MessagePriority.High).Build();
        Assert.Equal(MessagePriority.High, msg.Priority);
    }

    [Fact]
    public void Build_SetsDisableNotification()
    {
        var msg = CreateBuilder().ToChat(1).WithText("x").DisableNotification().Build();
        Assert.True(msg.DisableNotification);
    }

    [Fact]
    public void Build_SetsProtectContent()
    {
        var msg = CreateBuilder().ToChat(1).WithText("x").ProtectContent().Build();
        Assert.True(msg.ProtectContent);
    }

    [Fact]
    public void Build_SetsAdvancedSendOptions()
    {
        var msg = CreateBuilder()
            .ToChat(1)
            .WithText("x")
            .DisableLinkPreview()
            .ShowCaptionAboveMedia()
            .AllowPaidBroadcast()
            .WithMessageEffect("effect-1")
            .WithBusinessConnection("business-1")
            .Build();

        Assert.True(msg.DisableLinkPreview);
        Assert.True(msg.ShowCaptionAboveMedia);
        Assert.True(msg.AllowPaidBroadcast);
        Assert.Equal("effect-1", msg.MessageEffectId);
        Assert.Equal("business-1", msg.BusinessConnectionId);
    }

    [Fact]
    public void Build_SetsScheduledAt()
    {
        var time = DateTimeOffset.UtcNow.AddHours(1);
        var msg = CreateBuilder().ToChat(1).WithText("x").ScheduleAt(time).Build();
        Assert.Equal(time, msg.ScheduledAt);
    }

    [Fact]
    public void Build_SetsMedia()
    {
        var msg = CreateBuilder()
            .ToChat(1)
            .WithMedia(TelegramMediaType.Photo, "https://example.com/photo.jpg", "caption")
            .Build();

        Assert.Equal(TelegramMediaType.Photo, msg.MediaType);
        Assert.Equal("https://example.com/photo.jpg", msg.MediaUrl);
        Assert.Equal("caption", msg.MediaCaption);
    }

    [Fact]
    public void Build_SetsMediaAttachment()
    {
        var content = new byte[] { 1, 2, 3 };

        var msg = CreateBuilder()
            .ToChat(1)
            .WithMedia(TelegramMediaType.Document, content, "report.xlsx", "caption")
            .Build();

        Assert.Equal(TelegramMediaType.Document, msg.MediaType);
        Assert.Null(msg.MediaUrl);
        Assert.NotNull(msg.MediaAttachment);
        Assert.Equal("report.xlsx", msg.MediaAttachment.FileName);
        Assert.Equal(content, msg.MediaAttachment.Content);
        Assert.Equal("caption", msg.MediaCaption);
    }

    [Fact]
    public void Build_SetsReplyKeyboard()
    {
        var msg = CreateBuilder()
            .ToChat(1)
            .WithText("Choose:")
            .WithReplyKeyboard(
                [["A", "B"], ["C"]],
                true,
                true,
                "Pick one")
            .Build();

        Assert.NotNull(msg.ReplyKeyboard);
        Assert.Equal(2, msg.ReplyKeyboard.Buttons.Count);
        Assert.Equal(["A", "B"], msg.ReplyKeyboard.Buttons[0].Select(button => button.Text).ToArray());
        Assert.Equal(["C"], msg.ReplyKeyboard.Buttons[1].Select(button => button.Text).ToArray());
        Assert.True(msg.ReplyKeyboard.ResizeKeyboard);
        Assert.True(msg.ReplyKeyboard.OneTimeKeyboard);
        Assert.Equal("Pick one", msg.ReplyKeyboard.InputFieldPlaceholder);
    }

    [Fact]
    public void Build_SetsRichReplyKeyboard()
    {
        var msg = CreateBuilder()
            .ToChat(1)
            .WithText("Share")
            .WithReplyKeyboard(
                [
                    [
                        new ReplyKeyboardButtonData
                        {
                            Text = "Share contact",
                            Type = TelegramReplyButtonType.RequestContact,
                            Style = TelegramButtonStyle.Primary,
                            IconCustomEmojiId = "5395601891781224729"
                        }
                    ],
                    [
                        new ReplyKeyboardButtonData
                        {
                            Text = "Pick team",
                            Type = TelegramReplyButtonType.RequestUsers,
                            RequestUsers = new KeyboardButtonRequestUsers(7)
                            {
                                UserIsPremium = true,
                                MaxQuantity = 2,
                                RequestName = true
                            }
                        }
                    ]
                ],
                true,
                false,
                "Share with the bot",
                true)
            .Build();

        Assert.NotNull(msg.ReplyKeyboard);
        Assert.True(msg.ReplyKeyboard.IsPersistent);
        Assert.Equal(TelegramReplyButtonType.RequestContact, msg.ReplyKeyboard.Buttons[0][0].Type);
        Assert.Equal(TelegramButtonStyle.Primary, msg.ReplyKeyboard.Buttons[0][0].Style);
        Assert.Equal("5395601891781224729", msg.ReplyKeyboard.Buttons[0][0].IconCustomEmojiId);
        Assert.Equal(7, msg.ReplyKeyboard.Buttons[1][0].RequestUsers?.RequestId);
    }

    [Fact]
    public void Build_SetsExplicitTextAndCaptionEntities()
    {
        var textEntities = new[]
        {
            new MessageEntity { Type = MessageEntityType.Bold, Offset = 0, Length = 6 }
        };
        var captionEntities = new[]
        {
            new MessageEntity { Type = MessageEntityType.Code, Offset = 7, Length = 5 }
        };

        var msg = CreateBuilder()
            .ToChat(1)
            .WithTextEntities("Status: ready", textEntities)
            .WithMedia(TelegramMediaType.Photo, "https://example.com/photo.jpg", "Photo ready")
            .WithCaptionEntities("Photo ready", captionEntities)
            .Build();

        Assert.NotNull(msg.TextEntities);
        Assert.Single(msg.TextEntities);
        Assert.Equal(MessageEntityType.Bold, msg.TextEntities[0].Type);
        Assert.NotNull(msg.CaptionEntities);
        Assert.Single(msg.CaptionEntities);
        Assert.Equal(MessageEntityType.Code, msg.CaptionEntities[0].Type);
    }

    [Fact]
    public void Build_SetsForceReply()
    {
        var msg = CreateBuilder()
            .ToChat(1)
            .WithText("Enter name:")
            .WithForceReply("Your name")
            .Build();

        Assert.NotNull(msg.ForceReply);
        Assert.Equal("Your name", msg.ForceReply.InputFieldPlaceholder);
    }

    [Fact]
    public void Build_SetsRemoveReplyKeyboard()
    {
        var msg = CreateBuilder()
            .ToChat(1)
            .WithText("Done")
            .RemoveReplyKeyboard()
            .Build();

        Assert.True(msg.RemoveReplyKeyboard);
    }

    [Fact]
    public void Build_SetsParseMode()
    {
        var msg = CreateBuilder()
            .ToChat(1)
            .WithText("*bold*")
            .WithParseMode(TelegramParseMode.MarkdownV2)
            .Build();

        Assert.Equal(TelegramParseMode.MarkdownV2, msg.ParseMode);
    }

    [Fact]
    public void Build_SetsKeyboardItems()
    {
        var items = new List<KeyboardItemData>
        {
            KeyboardItemData.FromValues(("text", "Item1"), ("value", "v1")),
            KeyboardItemData.FromValues(("text", "Item2"), ("value", "v2"))
        };

        var msg = CreateBuilder()
            .ToChat(1)
            .WithTemplate("t")
            .WithKeyboardItems("products", items, 2)
            .Build();

        Assert.NotNull(msg.DynamicButtonData);
        Assert.Equal(2, msg.DynamicButtonData["products"].Count);
        Assert.NotNull(msg.DynamicButtonPages);
        Assert.Equal(2, msg.DynamicButtonPages["products"]);
    }

    [Fact]
    public void FluentChaining_ReturnsSameBuilder()
    {
        var builder = CreateBuilder();
        var result = builder
            .ToChat(1)
            .InThread(2)
            .WithText("x")
            .WithLanguage("en")
            .WithPriority(MessagePriority.Low)
            .DisableNotification()
            .ProtectContent();

        Assert.Same(builder, result);
    }

    [Fact]
    public async Task SendAsync_CallsSendPipeline()
    {
        var expected = SendResult.Ok(1, 10);
        _sendPipeline.SendAsync(Arg.Any<SendContext>(), Arg.Any<CancellationToken>())
            .Returns(expected);
        _botProvider.GetConfiguration("test-bot")
            .Returns(new BotConfiguration { Key = "test-bot", Token = "tok" });

        var result = await CreateBuilder()
            .ToChat(1)
            .WithText("hi")
            .SendAsync();

        Assert.True(result.Success);
        await _sendPipeline.Received(1).SendAsync(Arg.Any<SendContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueueAsync_EnqueuesMessage()
    {
        var result = await CreateBuilder()
            .ToChat(1)
            .WithText("hi")
            .QueueAsync();

        Assert.True(result.Success);
        await _queueBackend.Received(1).EnqueueAsync(Arg.Any<QueuedTelegramMessage>(), Arg.Any<CancellationToken>());
    }
}
