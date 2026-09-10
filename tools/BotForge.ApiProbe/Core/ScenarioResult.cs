namespace BotForge.ApiProbe.Core;

public sealed class ScenarioResult
{
    public required string ScenarioName { get; init; }
    public required string Description { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset FinishedAt { get; init; }
    public required TimeSpan Duration { get; init; }

    public required int TotalRequests { get; init; }
    public required int Succeeded { get; init; }
    public required int RateLimited { get; init; }
    public required int OtherErrors { get; init; }

    public required double ActualRps { get; init; }
    public double? TargetRps { get; init; }
    public double? ThresholdRps { get; init; }

    public required double AvgLatencyMs { get; init; }
    public required double P50LatencyMs { get; init; }
    public required double P95LatencyMs { get; init; }
    public required double P99LatencyMs { get; init; }
    public double? MaxRetryAfterSeconds { get; init; }
    public long? TotalPayloadBytes { get; init; }
    public double? AvgBytesPerSecond { get; init; }

    public required IReadOnlyList<RequestRecord> Requests { get; init; }

    public static ScenarioResult FromRequests(
        string name,
        string description,
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt,
        IReadOnlyList<RequestRecord> requests,
        double? targetRps = null,
        double? thresholdRps = null)
    {
        var duration = finishedAt - startedAt;
        var succeeded = requests.Count(r => !r.IsRateLimited && r.ErrorMessage is null);
        var rateLimited = requests.Count(r => r.IsRateLimited);
        var otherErrors = requests.Count(r => !r.IsRateLimited && r.ErrorMessage is not null);

        var latencies = requests.Select(r => r.Latency.TotalMilliseconds).OrderBy(l => l).ToList();
        var avgLatency = latencies.Count > 0 ? latencies.Average() : 0;

        return new ScenarioResult
        {
            ScenarioName = name,
            Description = description,
            StartedAt = startedAt,
            FinishedAt = finishedAt,
            Duration = duration,
            TotalRequests = requests.Count,
            Succeeded = succeeded,
            RateLimited = rateLimited,
            OtherErrors = otherErrors,
            ActualRps = duration.TotalSeconds > 0 ? requests.Count / duration.TotalSeconds : 0,
            TargetRps = targetRps,
            ThresholdRps = thresholdRps,
            AvgLatencyMs = avgLatency,
            P50LatencyMs = Percentile(latencies, 0.50),
            P95LatencyMs = Percentile(latencies, 0.95),
            P99LatencyMs = Percentile(latencies, 0.99),
            MaxRetryAfterSeconds = requests
                .Where(r => r.RetryAfterSeconds.HasValue)
                .Select(r => (double)r.RetryAfterSeconds!.Value)
                .DefaultIfEmpty(0)
                .Max(),
            TotalPayloadBytes = requests.Any(r => r.PayloadBytes.HasValue)
                ? requests.Sum(r => r.PayloadBytes ?? 0)
                : null,
            AvgBytesPerSecond = requests.Any(r => r.PayloadBytes.HasValue) && duration.TotalSeconds > 0
                ? requests.Sum(r => r.PayloadBytes ?? 0) / duration.TotalSeconds
                : null,
            Requests = requests
        };
    }

    private static double Percentile(List<double> sorted, double p)
    {
        if (sorted.Count == 0) return 0;
        var index = (int)Math.Ceiling(p * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }
}
