using BotForge.Core;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace BotForge.Messaging.Commands;

public sealed class TelegramCommandSyncService(
    ITelegramBotClientProvider botClientProvider,
    ILogger<TelegramCommandSyncService> logger) : ITelegramCommandSyncService
{
    public async Task SynchronizeAsync(
        string botId,
        IReadOnlyCollection<TelegramCommandSet> commandSets,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(botId))
            throw new ArgumentException("Bot id is required.", nameof(botId));

        ArgumentNullException.ThrowIfNull(commandSets);

        var client = botClientProvider.GetClient(botId);
        var normalized = commandSets
            .Distinct(TelegramCommandSetComparer.Instance)
            .OrderBy(x => DescribeScope(x.Scope), StringComparer.Ordinal)
            .ThenBy(x => x.LanguageCode ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var set in normalized)
        {
            var scopeLabel = DescribeScope(set.Scope);
            var language = set.LanguageCode ?? "<default>";

            if (set.Commands.Count == 0)
            {
                logger.LogInformation(
                    "Deleting Telegram commands for bot '{BotId}', scope '{Scope}', language '{LanguageCode}'",
                    botId,
                    scopeLabel,
                    language);
                await client.SendRequest(
                    new DeleteMyCommandsRequest
                    {
                        Scope = set.Scope,
                        LanguageCode = set.LanguageCode
                    },
                    cancellationToken).ConfigureAwait(false);
                continue;
            }

            logger.LogInformation(
                "Setting {Count} Telegram commands for bot '{BotId}', scope '{Scope}', language '{LanguageCode}'",
                set.Commands.Count,
                botId,
                scopeLabel,
                language);
            await client.SendRequest(
                new SetMyCommandsRequest
                {
                    Commands = set.Commands.ToArray(),
                    Scope = set.Scope,
                    LanguageCode = set.LanguageCode
                },
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static string DescribeScope(BotCommandScope scope) =>
        scope switch
        {
            BotCommandScopeDefault => "default",
            BotCommandScopeAllPrivateChats => "all_private_chats",
            BotCommandScopeAllGroupChats => "all_group_chats",
            BotCommandScopeAllChatAdministrators => "all_chat_administrators",
            BotCommandScopeChat chat => $"chat:{chat.ChatId}",
            BotCommandScopeChatAdministrators admins => $"chat_admins:{admins.ChatId}",
            BotCommandScopeChatMember member => $"chat_member:{member.ChatId}:{member.UserId}",
            _ => scope.Type.ToString()
        };

    private sealed class TelegramCommandSetComparer : IEqualityComparer<TelegramCommandSet>
    {
        public static readonly TelegramCommandSetComparer Instance = new();

        public bool Equals(TelegramCommandSet? x, TelegramCommandSet? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return string.Equals(x.LanguageCode, y.LanguageCode, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(DescribeScope(x.Scope), DescribeScope(y.Scope), StringComparison.Ordinal)
                   && AreCommandsEqual(x.Commands, y.Commands);
        }

        public int GetHashCode(TelegramCommandSet obj)
        {
            var hash = new HashCode();
            hash.Add(DescribeScope(obj.Scope), StringComparer.Ordinal);
            hash.Add(obj.LanguageCode ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            foreach (var command in obj.Commands.OrderBy(c => c.Command, StringComparer.Ordinal))
            {
                hash.Add(command.Command, StringComparer.Ordinal);
                hash.Add(command.Description, StringComparer.Ordinal);
            }

            return hash.ToHashCode();
        }

        private static bool AreCommandsEqual(IReadOnlyCollection<BotCommand> left, IReadOnlyCollection<BotCommand> right)
        {
            if (left.Count != right.Count) return false;

            var leftOrdered = left.OrderBy(x => x.Command, StringComparer.Ordinal).ToArray();
            var rightOrdered = right.OrderBy(x => x.Command, StringComparer.Ordinal).ToArray();

            for (var i = 0; i < leftOrdered.Length; i++)
                if (!string.Equals(leftOrdered[i].Command, rightOrdered[i].Command, StringComparison.Ordinal)
                    || !string.Equals(leftOrdered[i].Description, rightOrdered[i].Description, StringComparison.Ordinal))
                    return false;

            return true;
        }
    }
}
