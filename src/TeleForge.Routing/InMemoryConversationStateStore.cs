using System.Collections.Concurrent;
using TeleForge.Routing.Abstractions;

namespace TeleForge.Routing;

/// <summary>
/// In-memory conversation state store for single-instance deployments.
/// Uses ConcurrentDictionary keyed by (chatId, userId).
/// </summary>
public class InMemoryConversationStateStore : IConversationStateStore
{
    private readonly ConcurrentDictionary<(long ChatId, long UserId), ConversationStep> _states = new();

    public Task<ConversationStep?> GetAsync(long chatId, long userId, CancellationToken ct = default)
    {
        if (!_states.TryGetValue((chatId, userId), out var step)) return Task.FromResult<ConversationStep?>(null);

        if (!step.ExpiresAt.HasValue || step.ExpiresAt.Value >= DateTimeOffset.UtcNow)
            return Task.FromResult<ConversationStep?>(step);

        _states.TryRemove((chatId, userId), out _);
        return Task.FromResult<ConversationStep?>(null);
    }

    public Task SetAsync(long chatId, long userId, ConversationStep step, CancellationToken ct = default)
    {
        _states[(chatId, userId)] = step;
        return Task.CompletedTask;
    }

    public Task ClearAsync(long chatId, long userId, CancellationToken ct = default)
    {
        _states.TryRemove((chatId, userId), out _);
        return Task.CompletedTask;
    }
}
