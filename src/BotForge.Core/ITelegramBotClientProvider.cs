using Telegram.Bot;

namespace BotForge.Core;

public interface ITelegramBotClientProvider
{
    ITelegramBotClient GetClient(string botId);
    BotConfiguration GetConfiguration(string botId);
    IReadOnlyList<string> GetRegisteredBotKeys();
}
