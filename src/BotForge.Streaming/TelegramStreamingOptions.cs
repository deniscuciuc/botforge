namespace BotForge.Streaming;

/// <summary>
///     Configuration options for <see cref="TelegramStreamingExtensions.AddTelegramStreaming" />.
/// </summary>
public sealed class TelegramStreamingOptions
{
    internal const string Section = "TelegramStreaming";

    /// <summary>
    ///     The bot ID used when retrieving the <see cref="Telegram.Bot.ITelegramBotClient" />
    ///     from <see cref="BotForge.Core.ITelegramBotClientProvider" />.
    ///     Must match a key registered via <c>AddBot(botId, …)</c>.
    ///     Defaults to <c>"main"</c>.
    /// </summary>
    public string BotId { get; set; } = "main";
}
