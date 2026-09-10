using BotForge.Consumer.Hosting;
using BotForge.Consumer.Metrics;
using Telegram.Bot.Types;

namespace BotForge.Consumer.Webhook;

public class WebhookUpdateHandler
{
    private readonly UpdateChannel _channel;
    private readonly WebhookOptions _webhookOptions;

    internal WebhookUpdateHandler(UpdateChannel channel, WebhookOptions webhookOptions)
    {
        _channel = channel;
        _webhookOptions = webhookOptions;
    }

    /// <summary>
    /// Processes an incoming webhook update. Call this from your ASP.NET Core controller.
    /// Validates the secret token and enqueues the update for processing.
    /// </summary>
    public async Task HandleAsync(string botId, Update update, string? secretToken, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        if (!_webhookOptions.ValidateSecret(secretToken))
            throw new UnauthorizedAccessException("Invalid webhook secret token.");

        ConsumerMetrics.RecordIngested(botId, "webhook", update.Type.ToString());
        await _channel.Writer.WriteAsync(new TelegramUpdateEnvelope(botId, update, DateTimeOffset.UtcNow), ct).ConfigureAwait(false);
    }
}
