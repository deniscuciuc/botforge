using System.Collections.Concurrent;
using TeleForge.Routing.Abstractions;

namespace TeleForge.Routing;

/// <summary>
/// In-memory navigation service with per-user menu stacks.
/// </summary>
public class InMemoryNavigationService : INavigationService
{
    private readonly ConcurrentDictionary<(long ChatId, long UserId), Stack<NavigationEntry>> _stacks = new();

    public Task PushAsync(long chatId, long userId, NavigationEntry entry, CancellationToken ct = default)
    {
        var stack = _stacks.GetOrAdd((chatId, userId), _ => new Stack<NavigationEntry>());
        lock (stack)
        {
            stack.Push(entry);
        }

        return Task.CompletedTask;
    }

    public Task<NavigationEntry?> PopAsync(long chatId, long userId, CancellationToken ct = default)
    {
        if (!_stacks.TryGetValue((chatId, userId), out var stack))
            return Task.FromResult<NavigationEntry?>(null);

        lock (stack)
        {
            return Task.FromResult(stack.Count > 0 ? stack.Pop() : null);
        }
    }

    public Task<NavigationEntry?> PeekAsync(long chatId, long userId, CancellationToken ct = default)
    {
        if (!_stacks.TryGetValue((chatId, userId), out var stack))
            return Task.FromResult<NavigationEntry?>(null);

        lock (stack)
        {
            return Task.FromResult(stack.Count > 0 ? stack.Peek() : null);
        }
    }

    public Task ResetAsync(long chatId, long userId, CancellationToken ct = default)
    {
        _stacks.TryRemove((chatId, userId), out _);
        return Task.CompletedTask;
    }

    public Task<int> GetDepthAsync(long chatId, long userId, CancellationToken ct = default)
    {
        if (!_stacks.TryGetValue((chatId, userId), out var stack))
            return Task.FromResult(0);

        lock (stack)
        {
            return Task.FromResult(stack.Count);
        }
    }
}
