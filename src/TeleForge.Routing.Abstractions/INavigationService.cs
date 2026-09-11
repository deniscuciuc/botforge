namespace TeleForge.Routing.Abstractions;

/// <summary>
/// Provides menu navigation with a per-user stack.
/// </summary>
public interface INavigationService
{
    Task PushAsync(long chatId, long userId, NavigationEntry entry, CancellationToken ct = default);
    Task<NavigationEntry?> PopAsync(long chatId, long userId, CancellationToken ct = default);
    Task<NavigationEntry?> PeekAsync(long chatId, long userId, CancellationToken ct = default);
    Task ResetAsync(long chatId, long userId, CancellationToken ct = default);
    Task<int> GetDepthAsync(long chatId, long userId, CancellationToken ct = default);
}

/// <summary>
/// Represents a single position in the navigation stack.
/// </summary>
public class NavigationEntry
{
    public required string MenuName { get; init; }
    public Dictionary<string, string> State { get; init; } = [];
}
