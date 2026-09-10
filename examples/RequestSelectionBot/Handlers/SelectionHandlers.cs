using System.Net;
using BotForge.Core;
using BotForge.Messaging.Abstractions;
using BotForge.Routing.Attributes;
using BotForge.Routing.Handlers;
using Telegram.Bot.Types.ReplyMarkups;

namespace RequestSelectionBot.Handlers;

internal static class SelectionDemoKeyboard
{
    public const int UsersRequestId = 101;
    public const int ChatRequestId = 202;

    private const string PremiumUsersEmojiId = "5395601891781224729";
    private const string GroupChatEmojiId = "5395468751107323455";

    public static IReadOnlyList<IReadOnlyList<ReplyKeyboardButtonData>> Create()
    {
        return
        [
            [
                new ReplyKeyboardButtonData
                {
                    Text = "Pick premium users",
                    Type = TelegramReplyButtonType.RequestUsers,
                    Style = TelegramButtonStyle.Primary,
                    IconCustomEmojiId = PremiumUsersEmojiId,
                    RequestUsers = new KeyboardButtonRequestUsers(UsersRequestId)
                    {
                        UserIsPremium = true,
                        MaxQuantity = 3,
                        RequestName = true,
                        RequestUsername = true
                    }
                }
            ],
            [
                new ReplyKeyboardButtonData
                {
                    Text = "Pick a group chat",
                    Type = TelegramReplyButtonType.RequestChat,
                    Style = TelegramButtonStyle.Success,
                    IconCustomEmojiId = GroupChatEmojiId,
                    RequestChat = new KeyboardButtonRequestChat(ChatRequestId, false)
                    {
                        RequestTitle = true,
                        RequestUsername = true
                    }
                }
            ]
        ];
    }

    public static ITelegramMessage Apply(ITelegramMessage message)
    {
        return message.WithReplyKeyboard(
            Create(),
            true,
            false,
            "Choose a request button",
            true);
    }
}

[TelegramCommand("/start", "Show request selection demo")]
public class StartHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var builder = messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "🧪 <b>Request selection demo</b>\n\n" +
                "Use the reply keyboard below to request premium users or a group chat.\n" +
                "Telegram will send the result back as <code>UsersShared</code> or <code>ChatShared</code> service messages.\n\n" +
                "These request buttons work in private chats.");

        await SelectionDemoKeyboard.Apply(builder).SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/remove", "Remove the request keyboard")]
public class RemoveKeyboardHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("Request keyboard removed. Send /start to show it again.")
            .RemoveReplyKeyboard()
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

[UsersSharedMessage]
public class UsersSharedHandler(ITelegramMessageService messages) : IUsersSharedHandler
{
    public async Task HandleAsync(UsersSharedContext context, CancellationToken ct)
    {
        var users = context.Users.Count == 0
            ? "- No users were returned"
            : string.Join("\n", context.Users.Select(FormatSharedUser));

        var builder = messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                $"<b>UsersShared received</b>\n" +
                $"Request ID: <code>{context.RequestId}</code>\n" +
                $"Users:\n{users}\n\n" +
                "Use the same keyboard to try another selection.");

        await SelectionDemoKeyboard.Apply(builder).SendAsync(ct);
    }

    private static string FormatSharedUser(Telegram.Bot.Types.SharedUser user)
    {
        var identity = !string.IsNullOrEmpty(user.Username)
            ? $"@{WebUtility.HtmlEncode(user.Username)}"
            : WebUtility.HtmlEncode(string.Join(' ', new[] { user.FirstName, user.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value))));

        if (string.IsNullOrWhiteSpace(identity))
            identity = "(no name returned)";

        return $"- {identity} <code>{user.UserId}</code>";
    }
}

[ChatSharedMessage]
public class ChatSharedHandler(ITelegramMessageService messages) : IChatSharedHandler
{
    public async Task HandleAsync(ChatSharedContext context, CancellationToken ct)
    {
        var details = new List<string>
        {
            $"Chat ID: <code>{context.ChatIdValue}</code>"
        };

        if (!string.IsNullOrWhiteSpace(context.Title))
            details.Add($"Title: {WebUtility.HtmlEncode(context.Title)}");

        if (!string.IsNullOrWhiteSpace(context.Username))
            details.Add($"Username: @{WebUtility.HtmlEncode(context.Username)}");

        var builder = messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                $"<b>ChatShared received</b>\n" +
                $"Request ID: <code>{context.RequestId}</code>\n" +
                string.Join("\n", details) +
                "\n\nUse the same keyboard to try another selection.");

        await SelectionDemoKeyboard.Apply(builder).SendAsync(ct);
    }
}
