using System.Text;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Exceptions;

namespace BotForge.Streaming;

/// <summary>
///     Coordinates streaming AI tokens to a Telegram chat via <see cref="ITelegramDraftService" />.
///     Throttles draft updates so the typing animation is smooth without exhausting Bot API rate limits.
///     Uses <see cref="StreamingSessionGuard" /> to cancel any previous streaming session for the same
///     chat before starting a new one, preventing overlapping drafts.
/// </summary>
public sealed class StreamingResponseSender(
    ITelegramDraftService draftService,
    StreamingSessionGuard sessionGuard,
    ILogger<StreamingResponseSender> logger)
{
    /// <summary>Minimum interval between draft updates.</summary>
    private static readonly TimeSpan ThrottleInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>
    ///     Flush the draft when this many new characters have accumulated since the last update
    /// </summary>
    private const int ThrottleMinChars = 80;

    /// <summary>
    ///     Resend the draft before the 30-second TTL expires to keep it alive.
    ///     Using 25 s gives a comfortable safety margin.
    /// </summary>
    private static readonly TimeSpan DraftTtlWarning = TimeSpan.FromSeconds(25);

    /// <summary>
    ///     Streams <paramref name="tokens" /> to the chat as a live draft, flushing
    ///     periodically to produce a smooth typing effect.
    ///     Cancels any in-progress streaming for the same <paramref name="chatId" /> before starting.
    /// </summary>
    /// <returns>
    ///     The accumulated full text on success, or <see langword="null" /> when the initial
    ///     draft request failed — the caller should fall back to a standard batch message.
    /// </returns>
    public async Task<string?> TrySendAsync(
        long chatId,
        IAsyncEnumerable<string> tokens,
        CancellationToken ct)
    {
        // Cancel any in-progress streaming session for this chat before starting a new one.
        var sessionCts = sessionGuard.Begin(chatId, ct);
        var draftId = Random.Shared.Next(1, int.MaxValue);

        try
        {
            try
            {
                // Empty text, Telegram shows the built-in "Thinking…" placeholder.
                await draftService.SendDraftAsync(chatId, draftId, string.Empty, sessionCts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Initial draft failed for chatId={ChatId} — falling back to batch", chatId);
                return null;
            }

            var sb = new StringBuilder();
            var lastSentLength = 0;
            var lastSentAt = DateTime.UtcNow;
            var draftBornAt = DateTime.UtcNow;

            try
            {
                await foreach (var chunk in tokens.WithCancellation(sessionCts.Token))
                {
                    sessionCts.Token.ThrowIfCancellationRequested();
                    sb.Append(chunk);

                    var now = DateTime.UtcNow;
                    var charsSinceSent = sb.Length - lastSentLength;
                    var timeSinceSent = now - lastSentAt;
                    var draftAge = now - draftBornAt;

                    var shouldFlush = charsSinceSent >= ThrottleMinChars || timeSinceSent >= ThrottleInterval;
                    var mustRefreshTtl = draftAge >= DraftTtlWarning;

                    if ((shouldFlush || mustRefreshTtl) && sb.Length > 0)
                    {
                        try
                        {
                            await draftService.SendDraftAsync(chatId, draftId, sb.ToString(), sessionCts.Token).ConfigureAwait(false);
                            lastSentLength = sb.Length;
                            lastSentAt = DateTime.UtcNow;
                            if (mustRefreshTtl)
                                draftBornAt = DateTime.UtcNow;
                        }
                        catch (ApiRequestException rex) when (rex.ErrorCode == 429)
                        {
                            // Telegram is rate-limiting this chat. Advance lastSentAt by the
                            // mandated cooldown so the throttle logic naturally skips updates
                            // until Telegram is ready again. The stream keeps accumulating tokens;
                            // the caller's sendMessage delivers the full response regardless.
                            var retryAfterSeconds = rex.Parameters?.RetryAfter ?? 5;
                            lastSentAt = DateTime.UtcNow.AddSeconds(retryAfterSeconds);
                            logger.LogWarning(
                                "Draft rate-limited for chatId={ChatId}; pausing updates for {RetryAfter}s",
                                chatId, retryAfterSeconds);
                        }
                        catch (Exception ex)
                        {
                            // Non-fatal: continue accumulating; final sendMessage still sends full text.
                            logger.LogWarning(ex, "Draft update failed for chatId={ChatId}", chatId);
                        }
                    }
                }

                // Always send the final accumulated text so the draft reflects the complete response
                // before it expires and the permanent message appears.
                if (sb.Length > lastSentLength)
                {
                    try
                    {
                        await draftService.SendDraftAsync(chatId, draftId, sb.ToString(), sessionCts.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Final draft flush failed for chatId={ChatId}", chatId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Token streaming failed for chatId={ChatId}", chatId);
                if (sb.Length == 0)
                    return null;
                // Partial text was accumulated — let the caller send what we have.
            }

            return sb.ToString();
        }
        finally
        {
            sessionGuard.End(chatId, sessionCts);
        }
    }
}
