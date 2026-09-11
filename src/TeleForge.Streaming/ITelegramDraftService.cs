namespace TeleForge.Streaming;

/// <summary>
///     Sends partial message drafts to a Telegram private chat using the
///     <c>sendMessageDraft</c> Bot API method (Bot API 9.6+). Drafts are ephemeral
///     30-second previews; the caller must send a permanent <c>sendMessage</c>
///     once generation is complete.
/// </summary>
public interface ITelegramDraftService
{
    /// <summary>
    ///     Sends or updates a message draft in the specified chat.
    ///     An empty <paramref name="text" /> shows the built-in "Thinking…" placeholder.
    ///     Passing the same <paramref name="draftId" /> animates the update in-place.
    /// </summary>

    Task SendDraftAsync(long chatId, int draftId, string text, CancellationToken ct = default);
}
