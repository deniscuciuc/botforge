namespace TeleForge.Contracts;

/// <summary>
/// Shared health status report for inter-service diagnostics.
/// </summary>
public sealed record ServiceHealthReport
{
    public required string ServiceName { get; init; }
    public required ServiceHealthStatus Status { get; init; }
    public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.UtcNow;
    public Dictionary<string, string> Details { get; init; } = [];
}

public enum ServiceHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}
