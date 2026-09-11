using TeleForge.Core;
using TeleForge.Messaging.Abstractions;
using TeleForge.Routing.Abstractions;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Handlers;
using TeleForge.Templates;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MultiLanguageBot.Handlers;

internal static class MultiLanguageBotTexts
{
    public const string PremiumIconEmojiId = "5395601891781224729";

    public static string SharePrompt(string lang)
    {
        return lang switch
        {
            "ru" => "Поделиться с ботом: отправьте контакт или геолокацию",
            "uk" => "Поділитися з ботом: надішліть контакт або геолокацію",
            _ => "Share with the bot: send a contact or location"
        };
    }

    public static string ContactReceived(string lang, string phoneNumber)
    {
        return lang switch
        {
            "ru" => $"Контакт получен: {phoneNumber}",
            "uk" => $"Контакт отримано: {phoneNumber}",
            _ => $"Contact received: {phoneNumber}"
        };
    }

    public static string LocationReceived(string lang, double latitude, double longitude)
    {
        return lang switch
        {
            "ru" => $"Геолокация получена: {latitude:F4}, {longitude:F4}",
            "uk" => $"Геолокацію отримано: {latitude:F4}, {longitude:F4}",
            _ => $"Location received: {latitude:F4}, {longitude:F4}"
        };
    }

    public static string ShareContactButton(string lang)
    {
        return lang switch
        {
            "ru" => "Поделиться контактом",
            "uk" => "Надіслати контакт",
            _ => "Share contact"
        };
    }

    public static string ShareLocationButton(string lang)
    {
        return lang switch
        {
            "ru" => "Поделиться геолокацией",
            "uk" => "Надіслати геолокацію",
            _ => "Share location"
        };
    }
}

[TelegramCommand("/start", "Show welcome message")]
public class StartHandler(
    ITelegramMessageService messages,
    ITemplateRenderer renderer,
    IUserLocaleResolver localeResolver) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var telegramLang = context.UpdateContext.RawUpdate.Message?.From?.LanguageCode;
        var lang = await localeResolver.ResolveLocaleAsync(context.UserId ?? 0, telegramLang, ct);
        var text = renderer.Render("welcome", new Dictionary<string, string>(), lang);

        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(text)
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/lang", "Change language")]
public class LanguageHandler(
    ITelegramMessageService messages,
    ITemplateRenderer renderer,
    IUserLocaleResolver localeResolver) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var telegramLang = context.UpdateContext.RawUpdate.Message?.From?.LanguageCode;
        var lang = await localeResolver.ResolveLocaleAsync(context.UserId ?? 0, telegramLang, ct);
        var text = renderer.Render("choose_lang", new Dictionary<string, string>(), lang);

        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText(text)
            .WithReplyKeyboard(
                [
                    [
                        new ReplyKeyboardButtonData
                        {
                            Text = "🇬🇧 English",
                            Style = TelegramButtonStyle.Primary,
                            IconCustomEmojiId = MultiLanguageBotTexts.PremiumIconEmojiId
                        },
                        new ReplyKeyboardButtonData
                        {
                            Text = "🇷🇺 Русский",
                            Style = TelegramButtonStyle.Success
                        },
                        new ReplyKeyboardButtonData
                        {
                            Text = "🇺🇦 Українська",
                            Style = TelegramButtonStyle.Danger
                        }
                    ]
                ],
                true,
                true,
                isPersistent: true)
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/share", "Request contact or location")]
public class ShareHandler(
    ITelegramMessageService messages,
    IUserLocaleResolver localeResolver) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var telegramLang = context.UpdateContext.RawUpdate.Message?.From?.LanguageCode;
        var lang = await localeResolver.ResolveLocaleAsync(context.UserId ?? 0, telegramLang, ct);
        var prompt = MultiLanguageBotTexts.SharePrompt(lang);
        var headerLength = prompt.IndexOf(':');
        if (headerLength < 0)
            headerLength = prompt.Length;

        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithTextEntities(
                prompt,
                [new MessageEntity { Type = MessageEntityType.Bold, Offset = 0, Length = headerLength }])
            .WithReplyKeyboard(
                [
                    [
                        new ReplyKeyboardButtonData
                        {
                            Text = MultiLanguageBotTexts.ShareContactButton(lang),
                            Type = TelegramReplyButtonType.RequestContact,
                            Style = TelegramButtonStyle.Primary,
                            IconCustomEmojiId = MultiLanguageBotTexts.PremiumIconEmojiId
                        }
                    ],
                    [
                        new ReplyKeyboardButtonData
                        {
                            Text = MultiLanguageBotTexts.ShareLocationButton(lang),
                            Type = TelegramReplyButtonType.RequestLocation,
                            Style = TelegramButtonStyle.Success
                        }
                    ]
                ],
                true,
                true,
                isPersistent: true)
            .SendAsync(ct);

        return CommandResult.Ok();
    }
}

[ContactMessage]
public class ShareContactHandler(
    ITelegramMessageService messages,
    IUserLocaleResolver localeResolver) : IContactHandler
{
    public async Task HandleAsync(ContactContext context, CancellationToken ct)
    {
        var telegramLang = context.UpdateContext.RawUpdate.Message?.From?.LanguageCode;
        var lang = await localeResolver.ResolveLocaleAsync(context.UserId ?? 0, telegramLang, ct);
        var text = MultiLanguageBotTexts.ContactReceived(lang, context.PhoneNumber);
        var prefixLength = text.IndexOf(':');
        if (prefixLength < 0)
            prefixLength = text.Length;

        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithTextEntities(
                text,
                [
                    new MessageEntity { Type = MessageEntityType.Bold, Offset = 0, Length = prefixLength },
                    new MessageEntity
                    {
                        Type = MessageEntityType.Code, Offset = prefixLength + 2, Length = context.PhoneNumber.Length
                    }
                ])
            .RemoveReplyKeyboard()
            .SendAsync(ct);
    }
}

[LocationMessage]
public class ShareLocationHandler(
    ITelegramMessageService messages,
    IUserLocaleResolver localeResolver) : ILocationHandler
{
    public async Task HandleAsync(LocationContext context, CancellationToken ct)
    {
        var telegramLang = context.UpdateContext.RawUpdate.Message?.From?.LanguageCode;
        var lang = await localeResolver.ResolveLocaleAsync(context.UserId ?? 0, telegramLang, ct);
        var coordinates = $"{context.Latitude:F4}, {context.Longitude:F4}";
        var text = MultiLanguageBotTexts.LocationReceived(lang, context.Latitude, context.Longitude);
        var prefixLength = text.IndexOf(':');
        if (prefixLength < 0)
            prefixLength = text.Length;

        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithTextEntities(
                text,
                [
                    new MessageEntity { Type = MessageEntityType.Bold, Offset = 0, Length = prefixLength },
                    new MessageEntity
                        { Type = MessageEntityType.Code, Offset = prefixLength + 2, Length = coordinates.Length }
                ])
            .RemoveReplyKeyboard()
            .SendAsync(ct);
    }
}

[TextMessage]
public class LanguageSelectionHandler(
    ITelegramMessageService messages,
    ITemplateRenderer renderer,
    UserLocaleStore localeStore) : ITextMessageHandler
{
    private static readonly Dictionary<string, string> LanguageMap = new()
    {
        ["🇬🇧 English"] = "en",
        ["🇷🇺 Русский"] = "ru",
        ["🇺🇦 Українська"] = "uk"
    };

    public async Task<TextMessageResult> HandleAsync(TextMessageContext context, CancellationToken ct)
    {
        if (LanguageMap.TryGetValue(context.Text, out var locale))
        {
            localeStore.SetLocale(context.UserId ?? 0, locale);
            var text = renderer.Render("lang_changed", new Dictionary<string, string>(), locale);

            await messages.CreateMessage(context.BotId)
                .ToChat(context.ChatId!.Value)
                .WithHtml(text)
                .RemoveReplyKeyboard()
                .SendAsync(ct);
            return TextMessageResult.Ok();
        }

        // Echo non-language messages with locale-aware template rendering
        var telegramLang = context.UpdateContext.RawUpdate.Message?.From?.LanguageCode;
        var currentLang = await localeStore.ResolveLocaleAsync(context.UserId ?? 0, telegramLang, ct);
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText(context.Text)
            .WithLanguage(currentLang)
            .SendAsync(ct);
        return TextMessageResult.Ok();
    }
}
