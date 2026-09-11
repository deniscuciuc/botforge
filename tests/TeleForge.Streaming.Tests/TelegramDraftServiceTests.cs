using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TeleForge.Core;
using TeleForge.Streaming;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace TeleForge.Streaming.Tests;

public sealed class TelegramDraftServiceTests
{
    private readonly ITelegramBotClientProvider _provider = Substitute.For<ITelegramBotClientProvider>();
    private readonly ITelegramBotClient _client = Substitute.For<ITelegramBotClient>();
    private readonly ILogger<TelegramDraftService> _logger = Substitute.For<ILogger<TelegramDraftService>>();

    private TelegramDraftService Create(string botId = "main") =>
        new(_provider, Options.Create(new TelegramStreamingOptions { BotId = botId }), _logger);

    [Fact]
    public async Task SendDraftAsync_ResolvesClientWithConfiguredBotId()
    {
        _provider.GetClient("my-bot").Returns(_client);
        var service = Create(botId: "my-bot");

        // NSubstitute returns default (Task.CompletedTask) for async methods on the substituted client,
        // so the extension method will complete without throwing on the mock client.
        await service.SendDraftAsync(chatId: 1, draftId: 42, text: "hello");

        _provider.Received(1).GetClient("my-bot");
    }

    [Fact]
    public async Task SendDraftAsync_UsesDefaultBotId_WhenNotConfigured()
    {
        _provider.GetClient("main").Returns(_client);
        var service = Create();

        await service.SendDraftAsync(chatId: 1, draftId: 1, text: "hi");

        _provider.Received(1).GetClient("main");
    }

    [Fact]
    public async Task SendDraftAsync_PropagatesException_FromProvider()
    {
        var ex = new InvalidOperationException("bot not registered");
        _provider.GetClient(Arg.Any<string>()).Throws(ex);
        var service = Create();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendDraftAsync(1, 1, "text"));

        Assert.Same(ex, thrown);
    }

    [Fact]
    public async Task SendDraftAsync_LogsWarning_OnProviderException()
    {
        _provider.GetClient(Arg.Any<string>()).Throws(new Exception("API error"));
        var service = Create();

        await Assert.ThrowsAsync<Exception>(() => service.SendDraftAsync(1, 1, "text"));

        _logger.ReceivedWithAnyArgs(1).Log(
            default,
            default,
            default!,
            default,
            default!);
    }

    [Fact]
    public async Task SendDraftAsync_RethrowsOperationCanceledException_WithoutLogging()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // The provider itself throws OCE (e.g. the call is cancelled before reaching Telegram).
        _provider.GetClient(Arg.Any<string>())
            .Returns(_ => throw new OperationCanceledException(cts.Token));
        var service = Create();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.SendDraftAsync(1, 1, "text", cts.Token));

        // OperationCanceledException must be rethrown, not swallowed into LogWarning.
        _logger.DidNotReceiveWithAnyArgs().Log(
            default,
            default,
            default!,
            default,
            default!);
    }

    // ── text truncation ───────────────────────────────────────────────────────

    [Fact]
    public async Task SendDraftAsync_CompletesWithoutException_WhenTextExceedsMaxLength()
    {
        _provider.GetClient(Arg.Any<string>()).Returns(_client);
        var service = Create();
        var oversizedText = new string('a', TelegramDraftService.MaxDraftTextLength + 500);

        // Truncation should happen silently; no exception should escape.
        await service.SendDraftAsync(chatId: 1, draftId: 1, text: oversizedText);
    }

    [Fact]
    public async Task SendDraftAsync_CompletesWithoutException_WhenTextAtExactLimit()
    {
        _provider.GetClient(Arg.Any<string>()).Returns(_client);
        var service = Create();
        var exactText = new string('a', TelegramDraftService.MaxDraftTextLength);

        // Text at the exact limit must not be truncated or throw.
        await service.SendDraftAsync(chatId: 1, draftId: 1, text: exactText);
    }

    // ── 429 rate-limit propagation ────────────────────────────────────────────

    [Fact]
    public async Task SendDraftAsync_RethrowsApiRequestException_WithoutWarningLog()
    {
        // Simulate a 429 response from the provider.
        var rex = new ApiRequestException(
            "Too Many Requests: retry after 5",
            429,
            new ResponseParameters { RetryAfter = 5 });
        _provider.GetClient(Arg.Any<string>()).Returns(_ => throw rex);
        var service = Create();

        var thrown = await Assert.ThrowsAsync<ApiRequestException>(
            () => service.SendDraftAsync(1, 1, "text"));

        Assert.Same(rex, thrown);

        // 429 is expected behavior — TelegramDraftService must NOT log a Warning.
        // Backoff decisions belong to the caller (StreamingResponseSender).
        _logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
