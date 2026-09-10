namespace BotForge.Core;

public class SendResult
{
    public bool Success { get; init; }
    public int? MessageId { get; init; }
    public long? ChatId { get; init; }
    public string? Error { get; init; }
    public int? ErrorCode { get; init; }
    public TimeSpan? RetryAfter { get; init; }

    public static SendResult Ok(long chatId, int messageId)
    {
        return new SendResult { Success = true, ChatId = chatId, MessageId = messageId };
    }

    public static SendResult Failed(string error, int? errorCode = null)
    {
        return new SendResult { Success = false, Error = error, ErrorCode = errorCode };
    }

    public static SendResult RateLimited(TimeSpan retryAfter)
    {
        return new SendResult { Success = false, Error = "Rate limited", ErrorCode = 429, RetryAfter = retryAfter };
    }
}
