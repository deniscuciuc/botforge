using Telegram.Bot.Types;

namespace TeleForge.Messaging.Commands;

public interface ITelegramCommandSyncService
{
    Task SynchronizeAsync(
        string botId,
        IReadOnlyCollection<TelegramCommandSet> commandSets,
        CancellationToken cancellationToken = default);
}

public sealed record TelegramCommandSet(
    BotCommandScope Scope,
    string? LanguageCode,
    IReadOnlyCollection<BotCommand> Commands);

