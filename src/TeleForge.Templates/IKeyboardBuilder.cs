using TeleForge.Core;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleForge.Templates;

public interface IKeyboardBuilder
{
    InlineKeyboardMarkup? BuildInlineKeyboard(
        MessageTemplate template,
        IDictionary<string, string> parameters,
        string language,
        IDictionary<string, IReadOnlyList<KeyboardItemData>>? dynamicData = null,
        IDictionary<string, int>? dynamicPages = null);
}
