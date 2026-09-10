using System.Collections.Concurrent;

namespace BotForge.Streaming;

/// <summary>
///     Ensures only one active streaming session per private chat at a time.
///     When a new session begins for a chat, any in-progress session for the same chat
///     is cancelled immediately to prevent overlapping drafts.
/// </summary>
public sealed class StreamingSessionGuard
{
    private readonly ConcurrentDictionary<long, CancellationTokenSource> _active = new();

    public CancellationTokenSource Begin(long chatId, CancellationToken ct)
    {
        if (_active.TryRemove(chatId, out var previous))
        {
            previous.Cancel();
            previous.Dispose();
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _active[chatId] = cts;
        return cts;
    }

    public void End(long chatId, CancellationTokenSource cts)
    {
        ArgumentNullException.ThrowIfNull(cts);

        // TryRemove(KeyValuePair) only removes if both key and value match,
        // so a concurrent newer session's entry is left intact.
        _active.TryRemove(new KeyValuePair<long, CancellationTokenSource>(chatId, cts));
        cts.Dispose();
    }
}
