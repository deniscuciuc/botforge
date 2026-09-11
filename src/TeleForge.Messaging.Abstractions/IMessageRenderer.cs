using TeleForge.Core;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleForge.Messaging.Abstractions;

public interface IMessageRenderer
{
    RenderedMessageText RenderContent(string templateName, IDictionary<string, string> parameters, string language);

    RenderedMessageText RenderContent(
        string templateName,
        IDictionary<string, string> parameters,
        string language,
        IDictionary<string, IReadOnlyList<IDictionary<string, string>>>? loopData);

    string Render(string templateName, IDictionary<string, string> parameters, string language);

    string Render(
        string templateName,
        IDictionary<string, string> parameters,
        string language,
        IDictionary<string, IReadOnlyList<IDictionary<string, string>>>? loopData);

    InlineKeyboardMarkup? BuildKeyboard(
        string templateName,
        IDictionary<string, string> parameters,
        string language,
        IDictionary<string, IReadOnlyList<KeyboardItemData>>? dynamicData = null,
        IDictionary<string, int>? dynamicPages = null);
}
