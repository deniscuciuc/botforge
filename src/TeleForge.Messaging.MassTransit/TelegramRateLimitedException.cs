using System.Globalization;

namespace TeleForge.Messaging.MassTransit;

/// <summary>
/// Thrown when Telegram rate-limits a send. MassTransit's delayed redelivery catches this
/// and reschedules the message; <see cref="RetryAfter"/> carries the interval Telegram asked
/// for so a retry policy can honour it rather than guess.
/// </summary>
public sealed class TelegramRateLimitedException : Exception
{
    public TelegramRateLimitedException(TimeSpan retryAfter)
        : base(string.Format(
            CultureInfo.InvariantCulture,
            "Rate limited by Telegram. Retry after {0}s.",
            retryAfter.TotalSeconds))
    {
        RetryAfter = retryAfter;
    }

    public TelegramRateLimitedException()
    {
    }

    public TelegramRateLimitedException(string message) : base(message)
    {
    }

    public TelegramRateLimitedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>How long Telegram asked the caller to wait before retrying.</summary>
    public TimeSpan RetryAfter { get; }
}
