using System.Globalization;
using BotForge.Core;
using BotForge.Templates.Metrics;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotForge.Templates;

public class KeyboardBuilder(
    IEmojiRegistry emojis,
    ConditionalEvaluator conditionals,
    ILocalizationKeyResolver? localizationResolver = null)
    : IKeyboardBuilder
{
    public InlineKeyboardMarkup? BuildInlineKeyboard(
        MessageTemplate template,
        IDictionary<string, string> parameters,
        string language,
        IDictionary<string, IReadOnlyList<KeyboardItemData>>? dynamicData = null,
        IDictionary<string, int>? dynamicPages = null)
    {
        ArgumentNullException.ThrowIfNull(template);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var hasDynamicButtons = dynamicData?.Count > 0;

        try
        {
            var allButtons = new List<(int Row, int Order, InlineKeyboardButton Button)>();

            // 1. Static buttons
            foreach (var btn in template.Buttons)
            {
                if (!string.IsNullOrEmpty(btn.ShowIf) && !conditionals.EvaluateCondition(btn.ShowIf, parameters))
                    continue;

                var text = ResolveButtonText(btn.Text, language);

                text = emojis.ResolveEmojis(text);
                text = SubstituteParameters(text, parameters);

                var button = CreateButton(btn, text, parameters);
                allButtons.Add((btn.Row, btn.Order, button));
            }

            // 2. Dynamic buttons
            if (dynamicData != null)
                foreach (var dynBtn in template.DynamicButtons)
                {
                    if (!dynamicData.TryGetValue(dynBtn.Name, out var items) || items.Count == 0)
                        continue;

                    var currentPage = dynamicPages != null && dynamicPages.TryGetValue(dynBtn.Name, out var pg)
                        ? pg
                        : 1;
                    var totalPages = (int)Math.Ceiling((double)items.Count / dynBtn.ItemsPerPage);
                    currentPage = Math.Clamp(currentPage, 1, Math.Max(1, totalPages));

                    var pageItems = items
                        .Skip((currentPage - 1) * dynBtn.ItemsPerPage)
                        .Take(dynBtn.ItemsPerPage)
                        .ToList();

                    var currentRow = dynBtn.RowStart;
                    var itemInRow = 0;

                    foreach (var item in pageItems)
                    {
                        var mergedParams = new Dictionary<string, string>(parameters);
                        foreach (var kvp in item.Parameters)
                            mergedParams[kvp.Key] = kvp.Value;

                        var text = ResolveButtonText(dynBtn.Text, language);

                        text = emojis.ResolveEmojis(text);
                        text = SubstituteParameters(text, mergedParams);

                        var button = CreateButton(dynBtn, text, mergedParams);
                        allButtons.Add((currentRow, itemInRow, button));

                        itemInRow++;
                        if (itemInRow >= dynBtn.ItemsPerRow)
                        {
                            itemInRow = 0;
                            currentRow++;
                        }
                    }

                    // Pagination buttons
                    if (totalPages > 1 && dynBtn.PaginationRow.HasValue && dynBtn.PaginationButtons != null)
                    {
                        var paginationRow = dynBtn.PaginationRow.Value;
                        var prefix = dynBtn.PaginationCallbackPrefix ?? dynBtn.Name;

                        if (currentPage > 1)
                        {
                            var prevText = dynBtn.PaginationButtons.Previous.GetValueOrDefault(language)
                                           ?? dynBtn.PaginationButtons.Previous.Values.FirstOrDefault()
                                           ?? "◀️";
                            prevText = emojis.ResolveEmojis(prevText);
                            allButtons.Add((paginationRow, 0,
                                InlineKeyboardButton.WithCallbackData(prevText, $"{prefix}:{currentPage - 1}")));
                        }

                        if (!string.IsNullOrEmpty(dynBtn.PaginationButtons.Counter))
                        {
                            var counterText = dynBtn.PaginationButtons.Counter
                                .Replace("{Page}", currentPage.ToString(CultureInfo.InvariantCulture))
                                .Replace("{TotalPages}", totalPages.ToString(CultureInfo.InvariantCulture));
                            allButtons.Add((paginationRow, 1,
                                InlineKeyboardButton.WithCallbackData(counterText, "noop")));
                        }

                        if (currentPage < totalPages)
                        {
                            var nextText = dynBtn.PaginationButtons.Next.GetValueOrDefault(language)
                                           ?? dynBtn.PaginationButtons.Next.Values.FirstOrDefault()
                                           ?? "▶️";
                            nextText = emojis.ResolveEmojis(nextText);
                            allButtons.Add((paginationRow, 2,
                                InlineKeyboardButton.WithCallbackData(nextText, $"{prefix}:{currentPage + 1}")));
                        }
                    }
                }

            if (allButtons.Count == 0)
            {
                sw.Stop();
                TemplateMetrics.RecordKeyboardBuild(template.Name, hasDynamicButtons, "empty", sw.Elapsed);
                return null;
            }

            // Group by row, sort within each row
            var rows = allButtons
                .GroupBy(b => b.Row)
                .OrderBy(g => g.Key)
                .Select(g => g.OrderBy(b => b.Order).Select(b => b.Button).ToArray())
                .ToArray();

            sw.Stop();
            TemplateMetrics.RecordKeyboardBuild(template.Name, hasDynamicButtons, "success", sw.Elapsed);
            return new InlineKeyboardMarkup(rows);
        }
        catch
        {
            sw.Stop();
            TemplateMetrics.RecordKeyboardBuild(template.Name, hasDynamicButtons, "exception", sw.Elapsed);
            throw;
        }
    }

    private static InlineKeyboardButton CreateButton(
        MessageTemplateButton button,
        string text,
        IDictionary<string, string> parameters)
    {
        return CreateButton(
            text,
            SubstituteParameters(button.Value, parameters),
            button.Type,
            button.Style,
            SubstituteParametersIfPresent(button.IconCustomEmojiId, parameters),
            SubstituteParametersIfPresent(button.LoginForwardText, parameters),
            SubstituteParametersIfPresent(button.LoginBotUsername, parameters),
            button.LoginRequestWriteAccess);
    }

    private static InlineKeyboardButton CreateButton(
        MessageTemplateDynamicButton button,
        string text,
        IDictionary<string, string> parameters)
    {
        return CreateButton(
            text,
            SubstituteParameters(button.Value, parameters),
            button.Type,
            button.Style,
            SubstituteParametersIfPresent(button.IconCustomEmojiId, parameters),
            SubstituteParametersIfPresent(button.LoginForwardText, parameters),
            SubstituteParametersIfPresent(button.LoginBotUsername, parameters),
            button.LoginRequestWriteAccess);
    }

    private static InlineKeyboardButton CreateButton(
        string text,
        string value,
        TelegramButtonType type,
        TelegramButtonStyle? style,
        string? iconCustomEmojiId,
        string? loginForwardText,
        string? loginBotUsername,
        bool loginRequestWriteAccess)
    {
        var button = type switch
        {
            TelegramButtonType.Callback => InlineKeyboardButton.WithCallbackData(text, value),
            TelegramButtonType.Url => InlineKeyboardButton.WithUrl(text, value),
            TelegramButtonType.WebApp => InlineKeyboardButton.WithWebApp(text,
                new WebAppInfo(RequireValue(type, value))),
            TelegramButtonType.SwitchInline => InlineKeyboardButton.WithSwitchInlineQuery(text, value),
            TelegramButtonType.SwitchInlineCurrentChat => InlineKeyboardButton.WithSwitchInlineQueryCurrentChat(text,
                value),
            TelegramButtonType.Login => InlineKeyboardButton.WithLoginUrl(text, new LoginUrl
            {
                Url = RequireValue(type, value),
                ForwardText = loginForwardText,
                BotUsername = loginBotUsername,
                RequestWriteAccess = loginRequestWriteAccess
            }),
            TelegramButtonType.Pay => InlineKeyboardButton.WithPay(text),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        if (style.HasValue)
            button.Style = MapStyle(style.Value);

        if (!string.IsNullOrWhiteSpace(iconCustomEmojiId))
            button.IconCustomEmojiId = iconCustomEmojiId;

        return button;
    }

    private static string SubstituteParameters(string text, IDictionary<string, string> parameters)
    {
        return TemplateRenderer.SubstituteParameters(text, parameters);
    }

    private static string? SubstituteParametersIfPresent(string? text, IDictionary<string, string> parameters)
    {
        return string.IsNullOrEmpty(text) ? text : SubstituteParameters(text, parameters);
    }

    private string ResolveButtonText(MessageTemplateText templateText, string language)
    {
        var text = templateText.Translations.GetValueOrDefault(language)
                   ?? templateText.Translations.Values.FirstOrDefault()
                   ?? "?";

        if (localizationResolver is not null)
            text = localizationResolver.ResolveButtonText(templateText.LocalizationKey, text, language);

        return text;
    }

    private static string RequireValue(TelegramButtonType type, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Button type '{type}' requires a non-empty Value.");

        return value;
    }

    private static KeyboardButtonStyle MapStyle(TelegramButtonStyle style)
    {
        return style switch
        {
            TelegramButtonStyle.Primary => KeyboardButtonStyle.Primary,
            TelegramButtonStyle.Success => KeyboardButtonStyle.Success,
            TelegramButtonStyle.Danger => KeyboardButtonStyle.Danger,
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
        };
    }
}
