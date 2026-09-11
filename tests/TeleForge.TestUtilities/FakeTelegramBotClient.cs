using NSubstitute;
using Telegram.Bot;

namespace TeleForge.TestUtilities;

/// <summary>
/// Creates a mock ITelegramBotClient via NSubstitute for test assertions.
/// </summary>
public static class FakeTelegramBotClientFactory
{
    public static ITelegramBotClient Create()
    {
        return Substitute.For<ITelegramBotClient>();
    }
}
