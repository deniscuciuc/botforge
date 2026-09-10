using BotForge.Core;
using BotForge.Messaging.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotForge.Templates;

public class TemplateMessageRenderer(
    ITemplateRenderer renderer,
    IKeyboardBuilder keyboardBuilder,
    IMessageTemplateStore templateStore)
    : IMessageRenderer
{
    public string Render(string templateName, IDictionary<string, string> parameters, string language)
    {
        return RenderContent(templateName, parameters, language).Text;
    }

    public RenderedMessageText RenderContent(string templateName, IDictionary<string, string> parameters,
        string language)
    {
        return RenderContent(templateName, parameters, language, null);
    }

    public string Render(
        string templateName,
        IDictionary<string, string> parameters,
        string language,
        IDictionary<string, IReadOnlyList<IDictionary<string, string>>>? loopData)
    {
        return RenderContent(templateName, parameters, language, loopData).Text;
    }

    public RenderedMessageText RenderContent(
        string templateName,
        IDictionary<string, string> parameters,
        string language,
        IDictionary<string, IReadOnlyList<IDictionary<string, string>>>? loopData)
    {
        var template = templateStore.Get(templateName);
        var parseMode = template?.ParseMode ?? TelegramParseMode.Html;
        var rendered = renderer.Render(templateName, parameters, language, loopData);
        return TelegramTextEntityCompiler.CompileNullable(rendered, parseMode) ?? new RenderedMessageText();
    }

    public InlineKeyboardMarkup? BuildKeyboard(
        string templateName,
        IDictionary<string, string> parameters,
        string language,
        IDictionary<string, IReadOnlyList<KeyboardItemData>>? dynamicData = null,
        IDictionary<string, int>? dynamicPages = null)
    {
        var template = templateStore.Get(templateName);
        if (template == null)
            return null;

        return keyboardBuilder.BuildInlineKeyboard(template, parameters, language, dynamicData, dynamicPages);
    }
}
