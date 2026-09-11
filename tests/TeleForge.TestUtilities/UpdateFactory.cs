using TeleForge.Core;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleForge.TestUtilities;

/// <summary>
/// Builders for Telegram Update, Message, CallbackQuery test data.
/// </summary>
public static class UpdateFactory
{
    private static int _nextUpdateId = 1;

    public static Update CreateMessageUpdate(long chatId, long userId, string text)
    {
        return new Update
        {
            Id = _nextUpdateId++,
            Message = CreateMessage(chatId, userId, text)
        };
    }

    public static Update CreateCommandUpdate(long chatId, long userId, string command, string? args = null)
    {
        var text = args != null ? $"{command} {args}" : command;
        var msg = CreateMessage(chatId, userId, text);
        msg.Entities =
        [
            new MessageEntity
            {
                Type = MessageEntityType.BotCommand,
                Offset = 0,
                Length = command.Length
            }
        ];
        return new Update
        {
            Id = _nextUpdateId++,
            Message = msg
        };
    }

    public static Update CreateCallbackQueryUpdate(long chatId, long userId, string callbackData)
    {
        return new Update
        {
            Id = _nextUpdateId++,
            CallbackQuery = new CallbackQuery
            {
                Id = Guid.NewGuid().ToString("N")[..16],
                From = CreateUser(userId),
                Data = callbackData,
                Message = CreateMessage(chatId, userId, "")
            }
        };
    }

    public static Update CreateInlineQueryUpdate(long userId, string query)
    {
        return new Update
        {
            Id = _nextUpdateId++,
            InlineQuery = new InlineQuery
            {
                Id = Guid.NewGuid().ToString("N")[..16],
                From = CreateUser(userId),
                Query = query,
                Offset = ""
            }
        };
    }

    public static TelegramUpdateContext CreateContext(Update update, string botId = "test-bot")
    {
        return new TelegramUpdateContext(update, botId);
    }

    public static TelegramUpdateContext CreateCommandContext(
        long chatId, long userId, string command, string? args = null, string botId = "test-bot")
    {
        return CreateContext(CreateCommandUpdate(chatId, userId, command, args), botId);
    }

    public static TelegramUpdateContext CreateCallbackContext(
        long chatId, long userId, string callbackData, string botId = "test-bot")
    {
        return CreateContext(CreateCallbackQueryUpdate(chatId, userId, callbackData), botId);
    }

    private static Message CreateMessage(long chatId, long userId, string text)
    {
        return new Message
        {
            Chat = new Chat { Id = chatId, Type = ChatType.Private },
            From = CreateUser(userId),
            Text = text,
            Date = DateTime.UtcNow
        };
    }

    private static User CreateUser(long userId)
    {
        return new User
        {
            Id = userId,
            IsBot = false,
            FirstName = $"User{userId}",
            LanguageCode = "en"
        };
    }
}
