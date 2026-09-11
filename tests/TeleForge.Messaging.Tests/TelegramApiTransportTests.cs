using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TeleForge.Core;
using TeleForge.Messaging.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleForge.Messaging.Tests;

public class TelegramApiTransportTests
{
    private readonly ITelegramBotClientProvider _botProvider = Substitute.For<ITelegramBotClientProvider>();
    private readonly ITelegramBotClient _client = Substitute.For<ITelegramBotClient>();

    public TelegramApiTransportTests()
    {
        _botProvider.GetClient("main").Returns(_client);
    }

    private static bool MatchesRichReplyKeyboardRequest(SendMessageRequest request)
    {
        var keyboard = request.ReplyMarkup as ReplyKeyboardMarkup;
        var rows = keyboard?.Keyboard.Select(row => row.ToArray()).ToArray();
        var requestUsers = rows != null && rows.Length > 1 ? rows[1][0].RequestUsers : null;

        return request.Text == "Share safely" &&
               request.ParseMode == ParseMode.None &&
               request.Entities != null &&
               request.Entities.Count() == 2 &&
               keyboard != null &&
               keyboard.IsPersistent &&
               rows != null &&
               rows.Length == 2 &&
               rows[0][0].RequestContact &&
               rows[0][0].Style == KeyboardButtonStyle.Primary &&
               rows[0][0].IconCustomEmojiId == "5395601891781224729" &&
               requestUsers != null &&
               requestUsers.RequestId == 17 &&
               requestUsers.MaxQuantity == 2;
    }

    [Fact]
    public async Task InvokeAsync_SendsMessageWithCompiledEntitiesAndNewOptions()
    {
        _client.SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new Message { Id = 41 });

        var transport = new TelegramApiTransport(_botProvider, NullLogger<TelegramApiTransport>.Instance);
        var context = new SendContext
        {
            Bot = new BotConfiguration { Key = "main", Token = "token" },
            Message = new QueuedTelegramMessage
            {
                BotId = "main",
                ChatId = 123,
                ThreadId = 7,
                RawText = "<b>Premium</b> <tg-emoji emoji-id=\"5395601891781224729\">⭐</tg-emoji>",
                ParseMode = TelegramParseMode.Html,
                DisableLinkPreview = true,
                AllowPaidBroadcast = true,
                MessageEffectId = "effect-1",
                BusinessConnectionId = "business-1"
            },
            CancellationToken = CancellationToken.None
        };

        var result = await transport.InvokeAsync(context, _ => Task.FromResult(new SendResult { Success = false }));

        Assert.True(result.Success);
        await _client.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(request =>
                request.Text == "Premium ⭐" &&
                request.ParseMode == ParseMode.None &&
                request.MessageThreadId == 7 &&
                request.LinkPreviewOptions != null &&
                request.LinkPreviewOptions.IsDisabled &&
                request.AllowPaidBroadcast &&
                request.MessageEffectId == "effect-1" &&
                request.BusinessConnectionId == "business-1" &&
                request.Entities != null &&
                request.Entities.Count() == 2 &&
                request.Entities.Last().Type == MessageEntityType.CustomEmoji &&
                request.Entities.Last().CustomEmojiId == "5395601891781224729"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_SendsPhotoWithCaptionEntitiesAndCaptionOptions()
    {
        _client.SendRequest(Arg.Any<SendPhotoRequest>(), Arg.Any<CancellationToken>())
            .Returns(new Message { Id = 87 });

        var transport = new TelegramApiTransport(_botProvider, NullLogger<TelegramApiTransport>.Instance);
        var context = new SendContext
        {
            Bot = new BotConfiguration { Key = "main", Token = "token" },
            Message = new QueuedTelegramMessage
            {
                BotId = "main",
                ChatId = 123,
                ThreadId = 4,
                MediaType = TelegramMediaType.Photo,
                MediaUrl = "https://example.com/premium.jpg",
                MediaCaption = "<b>Launch</b> <tg-emoji emoji-id=\"123\">🚀</tg-emoji>",
                ParseMode = TelegramParseMode.Html,
                ShowCaptionAboveMedia = true,
                AllowPaidBroadcast = true,
                MessageEffectId = "effect-2",
                BusinessConnectionId = "business-2"
            },
            CancellationToken = CancellationToken.None
        };

        var result = await transport.InvokeAsync(context, _ => Task.FromResult(new SendResult { Success = false }));

        Assert.True(result.Success);
        await _client.Received(1).SendRequest(
            Arg.Is<SendPhotoRequest>(request =>
                request.Caption == "Launch 🚀" &&
                request.ParseMode == ParseMode.None &&
                request.MessageThreadId == 4 &&
                request.ShowCaptionAboveMedia &&
                request.AllowPaidBroadcast &&
                request.MessageEffectId == "effect-2" &&
                request.BusinessConnectionId == "business-2" &&
                request.CaptionEntities != null &&
                request.CaptionEntities.Count() == 2 &&
                request.CaptionEntities.Last().Type == MessageEntityType.CustomEmoji &&
                request.CaptionEntities.Last().CustomEmojiId == "123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_SendsExplicitEntitiesAndRichReplyKeyboard()
    {
        _client.SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new Message { Id = 99 });

        var transport = new TelegramApiTransport(_botProvider, NullLogger<TelegramApiTransport>.Instance);
        var context = new SendContext
        {
            Bot = new BotConfiguration { Key = "main", Token = "token" },
            Message = new QueuedTelegramMessage
            {
                BotId = "main",
                ChatId = 123,
                RawText = "Share safely",
                TextEntities =
                [
                    new MessageEntity { Type = MessageEntityType.Bold, Offset = 0, Length = 5 },
                    new MessageEntity
                        { Type = MessageEntityType.TextLink, Offset = 6, Length = 6, Url = "https://example.com" }
                ],
                ReplyKeyboard = new ReplyKeyboardData
                {
                    IsPersistent = true,
                    Buttons =
                    [
                        [
                            new ReplyKeyboardButtonData
                            {
                                Text = "Share phone",
                                Type = TelegramReplyButtonType.RequestContact,
                                Style = TelegramButtonStyle.Primary,
                                IconCustomEmojiId = "5395601891781224729"
                            }
                        ],
                        [
                            new ReplyKeyboardButtonData
                            {
                                Text = "Pick teammates",
                                Type = TelegramReplyButtonType.RequestUsers,
                                RequestUsers = new KeyboardButtonRequestUsers(17)
                                {
                                    UserIsPremium = true,
                                    MaxQuantity = 2,
                                    RequestName = true
                                }
                            }
                        ]
                    ]
                }
            },
            CancellationToken = CancellationToken.None
        };

        var result = await transport.InvokeAsync(context, _ => Task.FromResult(new SendResult { Success = false }));

        Assert.True(result.Success);
        await _client.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(request => MatchesRichReplyKeyboardRequest(request)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_SendsReplyToMessage()
    {
        _client.SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new Message { Id = 51 });

        var transport = new TelegramApiTransport(_botProvider, NullLogger<TelegramApiTransport>.Instance);
        var context = new SendContext
        {
            Bot = new BotConfiguration { Key = "main", Token = "token" },
            Message = new QueuedTelegramMessage
            {
                BotId = "main",
                ChatId = 123,
                RawText = "Reply",
                ReplyToMessageId = 88
            },
            CancellationToken = CancellationToken.None
        };

        var result = await transport.InvokeAsync(context, _ => Task.FromResult(new SendResult { Success = false }));

        Assert.True(result.Success);
        await _client.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(request =>
                request.Text == "Reply" &&
                request.ReplyParameters != null &&
                request.ReplyParameters.MessageId == 88),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_SendsDocumentAttachmentAndReply()
    {
        SendDocumentRequest? capturedRequest = null;
        _client.SendRequest(Arg.Any<SendDocumentRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedRequest = callInfo.Arg<SendDocumentRequest>();
                return new Message { Id = 77 };
            });

        var transport = new TelegramApiTransport(_botProvider, NullLogger<TelegramApiTransport>.Instance);
        var context = new SendContext
        {
            Bot = new BotConfiguration { Key = "main", Token = "token" },
            Message = new QueuedTelegramMessage
            {
                BotId = "main",
                ChatId = 123,
                MediaType = TelegramMediaType.Document,
                MediaAttachment = new TelegramMediaAttachment
                {
                    FileName = "report.xlsx",
                    Content = [1, 2, 3, 4]
                },
                MediaCaption = "Run: <code>abc</code>",
                ParseMode = TelegramParseMode.Html,
                ReplyToMessageId = 90
            },
            CancellationToken = CancellationToken.None
        };

        var result = await transport.InvokeAsync(context, _ => Task.FromResult(new SendResult { Success = false }));

        Assert.True(result.Success);
        await _client.Received(1).SendRequest(Arg.Any<SendDocumentRequest>(), Arg.Any<CancellationToken>());
        Assert.NotNull(capturedRequest);
        Assert.Equal("Run: <code>abc</code>", capturedRequest.Caption);
        Assert.Equal(ParseMode.Html, capturedRequest.ParseMode);
        var document = Assert.IsType<InputFileStream>(capturedRequest.Document);
        Assert.Equal("report.xlsx", document.FileName);
        Assert.NotNull(capturedRequest.ReplyParameters);
        Assert.Equal(90, capturedRequest.ReplyParameters.MessageId);
    }
}
