namespace BotForge.Routing.Abstractions;

/// <summary>
/// Stores conversation state per (chatId, userId) pair for multi-step flows.
/// </summary>
public interface IConversationStateStore
{
    Task<ConversationStep?> GetAsync(long chatId, long userId, CancellationToken ct = default);
    Task SetAsync(long chatId, long userId, ConversationStep step, CancellationToken ct = default);
    Task ClearAsync(long chatId, long userId, CancellationToken ct = default);
}
