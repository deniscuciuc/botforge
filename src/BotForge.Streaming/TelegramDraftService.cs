using BotForge.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace BotForge.Streaming;

/// <summary>
///     Default implementation of <see cref="ITelegramDraftService" /> that
///     delegates to <see cref="ITelegramBotClientProvider" />.
/// </summary>
public sealed class TelegramDraftService(
    ITelegramBotClientProvider botClientProvider,
    IOptions<TelegramStreamingOptions> options,
    ILogger<TelegramDraftService> logger) : ITelegramDraftService
{
    /// <summary>
    ///     Telegram Bot API hard limit for <c>sendMessageDraft</c> text.
    ///     Drafts are ephemeral previews; the caller must send the full text via <c>sendMessage</c>.
    /// </summary>
    public const int MaxDraftTextLength = 4096;

    public async Task SendDraftAsync(long chatId, int draftId, string text, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        // Telegram enforces a 4096-character limit on draft text.
        // Truncation is safe here because the draft is an ephemeral preview —
        // the caller's final sendMessage delivers the complete response.
        if (text.Length > MaxDraftTextLength)
        {
            logger.LogDebug(
                "Draft text truncated {From}\u2192{To} chars for chatId={ChatId}",
                text.Length, MaxDraftTextLength, chatId);
            text = text[..MaxDraftTextLength];
        }

        try
        {
            var client = botClientProvider.GetClient(options.Value.BotId);
            await client.SendMessageDraft(chatId, draftId, text, cancellationToken: ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (ApiRequestException)
        {
            // API errors (including 429 rate limits) are propagated without logging.
            // Rate-limit backoff is the caller's responsibility (see StreamingResponseSender).
            // Logging here would duplicate the caller's context-aware log entry.
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send message draft to chatId={ChatId}", chatId);
            throw;
        }
    }
}
