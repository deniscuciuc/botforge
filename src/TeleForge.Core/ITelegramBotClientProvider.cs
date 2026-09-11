using Telegram.Bot;

namespace TeleForge.Core;

public interface ITelegramBotClientProvider
{
    ITelegramBotClient GetClient(string botId);
    BotConfiguration GetConfiguration(string botId);
    IReadOnlyList<string> GetRegisteredBotKeys();
}
