using BotForge.Core;
using BotForge.Messaging.Abstractions;
using BotForge.Routing.Attributes;
using BotForge.Routing.Handlers;
using Telegram.Bot;
using Telegram.Bot.Types.InlineQueryResults;

namespace InlineBot.Handlers;

[TelegramCommand("/start", "Show inline bot info")]
public class StartHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(
                "🔍 <b>InlineBot</b>\n\n" +
                "Type <code>@YourBotName query</code> in any chat to search.\n\n" +
                "Try searching for: colors, animals, or any text!")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[InlineQuery]
public class SearchHandler(ITelegramBotClientProvider botProvider) : IInlineQueryHandler
{
    private static readonly Dictionary<string, (string Title, string Description, string Content)[]> Database =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["colors"] =
            [
                ("🔴 Red", "The color of passion",
                    "Red (#FF0000) is a warm, bold color associated with passion, love, and energy."),
                ("🟢 Green", "The color of nature",
                    "Green (#00FF00) represents nature, growth, harmony, and freshness."),
                ("🔵 Blue", "The color of sky",
                    "Blue (#0000FF) evokes calm, trust, and depth. It's the color of sky and ocean."),
                ("🟡 Yellow", "The color of sunshine",
                    "Yellow (#FFFF00) is associated with happiness, energy, and warmth.")
            ],
            ["animals"] =
            [
                ("🐱 Cat", "Domestic feline",
                    "Cats are small, furry, carnivorous mammals that are often kept as pets."),
                ("🐶 Dog", "Man's best friend",
                    "Dogs are loyal companions known for their friendliness and trainability."),
                ("🐦 Bird", "Feathered friend", "Birds are warm-blooded vertebrates with feathers and beaks.")
            ]
        };

    public async Task HandleAsync(InlineQueryContext context, CancellationToken ct)
    {
        var query = context.Query.Trim();
        var client = botProvider.GetClient(context.BotId);

        var results = new List<InlineQueryResult>();
        var id = 0;

        if (string.IsNullOrEmpty(query))
        {
            results.Add(new InlineQueryResultArticle($"hint", "Type a search query...",
                new InputTextMessageContent("Start typing to search for colors, animals, and more!")));
        }
        else
        {
            foreach (var (category, items) in Database)
                foreach (var (title, description, content) in items)
                    if (category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        description.Contains(query, StringComparison.OrdinalIgnoreCase))
                        results.Add(new InlineQueryResultArticle(
                            $"r{id++}",
                            title,
                            new InputTextMessageContent($"<b>{title}</b>\n\n{content}")
                            { ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html })
                        {
                            Description = description
                        });

            if (results.Count == 0)
                results.Add(new InlineQueryResultArticle("noresult", $"No results for '{query}'",
                    new InputTextMessageContent($"No results found for: {query}")));
        }

        await client.AnswerInlineQuery(context.InlineQuery.Id, results, 10, cancellationToken: ct);
    }
}

[ChosenInlineResult]
public class ChosenResultHandler : IChosenInlineResultHandler
{
    public Task HandleAsync(ChosenInlineResultContext context, CancellationToken ct)
    {
        Console.WriteLine($"User {context.UserId} chose result '{context.ResultId}' with query '{context.Query}'");
        return Task.CompletedTask;
    }
}
