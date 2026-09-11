using TeleForge.Streaming;

namespace TeleForge.Streaming.Tests;

public sealed class StreamingSessionGuardTests
{
    private readonly StreamingSessionGuard _guard = new();

    [Fact]
    public void Begin_ReturnsLinkedCts_ThatCancelsWithOriginal()
    {
        using var original = new CancellationTokenSource();
        var session = _guard.Begin(chatId: 1, original.Token);

        Assert.False(session.Token.IsCancellationRequested);
        original.Cancel();
        Assert.True(session.Token.IsCancellationRequested);

        _guard.End(1, session);
    }

    [Fact]
    public void Begin_CancelsPreviousSession_WhenNewSessionStarts()
    {
        var first = _guard.Begin(chatId: 1, CancellationToken.None);
        var firstToken = first.Token; // capture before it is disposed

        // Starting a second session must cancel the first.
        var second = _guard.Begin(chatId: 1, CancellationToken.None);

        Assert.True(firstToken.IsCancellationRequested);
        Assert.False(second.Token.IsCancellationRequested);

        _guard.End(1, second);
    }

    [Fact]
    public void Begin_DoesNotAffectSession_ForDifferentChat()
    {
        var sessionA = _guard.Begin(chatId: 100, CancellationToken.None);
        var sessionB = _guard.Begin(chatId: 200, CancellationToken.None);

        Assert.False(sessionA.Token.IsCancellationRequested);
        Assert.False(sessionB.Token.IsCancellationRequested);

        _guard.End(100, sessionA);
        _guard.End(200, sessionB);
    }

    [Fact]
    public void End_DoesNotDisruptNewerSession_WhenOldSessionEndsLate()
    {
        var first = _guard.Begin(chatId: 1, CancellationToken.None);
        // Second Begin cancels and replaces the first.
        var second = _guard.Begin(chatId: 1, CancellationToken.None);

        // Stale End call from the old session must not remove the new one.
        _guard.End(1, first);

        // The new session's CTS must still be usable.
        Assert.False(second.IsCancellationRequested);
        _guard.End(1, second);
    }

    [Fact]
    public async Task ConcurrentBegin_IsThreadSafe()
    {
        const int threadCount = 20;
        var tasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
        {
            var cts = _guard.Begin(chatId: 42, CancellationToken.None);
            _guard.End(42, cts);
        }));

        // Must not throw ObjectDisposedException or other race-condition errors.
        await Task.WhenAll(tasks);
    }
}
