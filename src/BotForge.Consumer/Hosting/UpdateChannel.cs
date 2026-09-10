using System.Threading.Channels;
using Telegram.Bot.Types;

namespace BotForge.Consumer.Hosting;

internal sealed record TelegramUpdateEnvelope(string BotId, Update RawUpdate, DateTimeOffset EnqueuedAt);

internal sealed class UpdateChannel(int capacity)
{
    private readonly Channel<TelegramUpdateEnvelope> _channel = Channel.CreateBounded<TelegramUpdateEnvelope>(
        new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = false
        });

    public ChannelWriter<TelegramUpdateEnvelope> Writer => _channel.Writer;
    public ChannelReader<TelegramUpdateEnvelope> Reader => _channel.Reader;
}
