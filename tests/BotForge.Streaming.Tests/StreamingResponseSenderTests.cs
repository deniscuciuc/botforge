using System.Runtime.CompilerServices;
using BotForge.Streaming;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace BotForge.Streaming.Tests;

public sealed class StreamingResponseSenderTests
{
    private readonly ITelegramDraftService _draftService = Substitute.For<ITelegramDraftService>();
    private readonly ILogger<StreamingResponseSender> _logger = Substitute.For<ILogger<StreamingResponseSender>>();
    private readonly StreamingSessionGuard _guard = new();

    private StreamingResponseSender CreateSender() =>
        new(_draftService, _guard, _logger);

    // ── helpers ───────────────────────────────────────────────────────────────

    private static async IAsyncEnumerable<string> TokensFrom(
        params string[] tokens)
    {
        foreach (var token in tokens)
        {
            await Task.Yield();
            yield return token;
        }
    }

    // ── happy-path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrySendAsync_ReturnsAccumulatedText_OnSuccess()
    {
        var result = await CreateSender().TrySendAsync(1, TokensFrom("Hello", " ", "World"), CancellationToken.None);

        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async Task TrySendAsync_SendsInitialEmptyDraft_AsThinkingIndicator()
    {
        await CreateSender().TrySendAsync(42, TokensFrom("hi"), CancellationToken.None);

        await _draftService.Received(1)
            .SendDraftAsync(42, Arg.Any<int>(), string.Empty, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrySendAsync_SendsFinalFlush_ForShortContent()
    {
        // "hi" is well under the 80-char threshold, so no intermediate flush fires.
        await CreateSender().TrySendAsync(1, TokensFrom("hi"), CancellationToken.None);

        // Two calls: initial empty draft + final flush.
        await _draftService.Received(1)
            .SendDraftAsync(1, Arg.Any<int>(), string.Empty, Arg.Any<CancellationToken>());
        await _draftService.Received(1)
            .SendDraftAsync(1, Arg.Any<int>(), "hi", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrySendAsync_FlushesIntermediate_WhenCharThresholdReached()
    {
        // A single chunk larger than the 80-char threshold triggers an intermediate flush.
        var bigChunk = new string('x', 81);

        await CreateSender().TrySendAsync(1, TokensFrom(bigChunk), CancellationToken.None);

        await _draftService.Received()
            .SendDraftAsync(1, Arg.Any<int>(), bigChunk, Arg.Any<CancellationToken>());
    }

    // ── fallback ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrySendAsync_ReturnsNull_WhenInitialDraftFails()
    {
        _draftService
            .SendDraftAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("API unavailable"));

        var result = await CreateSender().TrySendAsync(1, TokensFrom("hello"), CancellationToken.None);

        Assert.Null(result);
    }

    // ── resilience ────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrySendAsync_ContinuesAccumulating_OnMidStreamDraftError()
    {
        var callCount = 0;
        _draftService
            .SendDraftAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                // Second call (first intermediate/final flush) throws — should be non-fatal.
                if (callCount == 2)
                    throw new Exception("throttled");
                return Task.CompletedTask;
            });

        var bigChunk = new string('x', 81);
        var result = await CreateSender().TrySendAsync(1, TokensFrom(bigChunk, " more"), CancellationToken.None);

        // Despite the mid-stream error the accumulated text is still returned.
        Assert.NotNull(result);
        Assert.StartsWith(bigChunk, result);
    }

    // ── cancellation ──────────────────────────────────────────────────────────

    [Fact]
    public async Task TrySendAsync_ContinuesAccumulating_WhenDraftUpdateRateLimited()
    {
        // Arrange: initial empty draft succeeds; all subsequent calls raise 429.
        var callCount = 0;
        var rex = new ApiRequestException(
            "Too Many Requests: retry after 1",
            429,
            new ResponseParameters { RetryAfter = 1 });

        _draftService
            .SendDraftAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                if (callCount > 1) throw rex; // only the initial empty draft succeeds
                return Task.CompletedTask;
            });

        var bigChunk = new string('x', 81); // exceeds ThrottleMinChars to trigger a flush attempt

        // Act
        var result = await CreateSender().TrySendAsync(1, TokensFrom(bigChunk, " tail"), CancellationToken.None);

        // Assert: despite 429 on every draft update, the full text is still returned
        Assert.Equal(bigChunk + " tail", result);
    }

    // ── cancellation ──────────────────────────────────────────────────────────

    [Fact]
    public async Task TrySendAsync_PropagatesCancellation_WhenTokenFiredMidStream()
    {
        using var cts = new CancellationTokenSource();

        async IAsyncEnumerable<string> CancelDuringStream(
            [EnumeratorCancellation] CancellationToken ct)
        {
            yield return "first";
            await cts.CancelAsync();
            await Task.Yield();
            yield return "second"; // unreachable
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateSender().TrySendAsync(1, CancelDuringStream(cts.Token), cts.Token));
    }

    [Fact]
    public async Task TrySendAsync_CancelsPreviousSession_WhenNewSessionBegins()
    {
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // A slow stream that blocks until its cancellation token fires.
        async IAsyncEnumerable<string> SlowStream(
            [EnumeratorCancellation] CancellationToken ct)
        {
            firstStarted.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct); // cancelled when second session begins
            yield return "never";
        }

        var sender = CreateSender();

        // Start the first (slow) session without awaiting it.
        var firstTask = sender.TrySendAsync(1, SlowStream(CancellationToken.None), CancellationToken.None);

        // Wait until the first session's stream is actively blocked.
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Second session for the same chat — StreamingSessionGuard cancels the first.
        var secondResult = await sender.TrySendAsync(1, TokensFrom("quick"), CancellationToken.None);

        Assert.Equal("quick", secondResult);

        // The first session must have been cancelled by the guard.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstTask);
    }
}
