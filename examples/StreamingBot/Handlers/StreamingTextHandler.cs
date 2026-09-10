using System.Runtime.CompilerServices;
using BotForge.Messaging.Abstractions;
using BotForge.Routing.Attributes;
using BotForge.Routing.Handlers;
using BotForge.Streaming;

namespace StreamingBot.Handlers;

/// <summary>
///     Demonstrates <see cref="StreamingResponseSender" /> by echoing the user's message
///     back word by word, simulating a live AI-style response stream.
/// </summary>
[TextMessage]
public sealed class StreamingTextHandler(
    ITelegramMessageService messages,
    StreamingResponseSender streamingSender) : ITextMessageHandler
{
    public async Task<TextMessageResult> HandleAsync(TextMessageContext context, CancellationToken ct)
    {
        var chatId = context.ChatId!.Value;

        // sendMessageDraft only works in private chats.
        if (chatId != context.UserId)
        {
            await messages.CreateMessage(context.BotId)
                .ToChat(chatId)
                .WithText("Streaming works in private chats only — message me directly!")
                .SendAsync(ct);
            return TextMessageResult.Ok();
        }

        var tokens = SimulateStreamAsync(context.Text ?? "Hello!", ct);
        var finalText = await streamingSender.TrySendAsync(chatId, tokens, ct);

        if (finalText is null)
        {
            // sendMessageDraft unavailable (client too old / not a private chat).
            // Fall back to a normal message.
            await messages.CreateMessage(context.BotId)
                .ToChat(chatId)
                .WithText($"Echo: {context.Text}")
                .SendAsync(ct);
            return TextMessageResult.Ok();
        }

        // Send the final permanent message, which replaces the expiring draft.
        await messages.CreateMessage(context.BotId)
            .ToChat(chatId)
            .WithText(finalText)
            .SendAsync(ct);

        return TextMessageResult.Ok();
    }

    /// <summary>
    ///     Yields the input text word by word with a short delay between each token,
    ///     mimicking a token stream from a language model.
    /// </summary>
    private static async IAsyncEnumerable<string> SimulateStreamAsync(
        string text,
        [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var word in text.Split(' '))
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(80, ct);
            yield return word + " ";
        }
    }
}
