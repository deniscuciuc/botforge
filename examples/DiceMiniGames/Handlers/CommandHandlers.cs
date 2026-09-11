using DiceMiniGames.Data;
using Microsoft.EntityFrameworkCore;
using TeleForge.Messaging.Abstractions;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Handlers;
using TeleForge.Templates;

namespace DiceMiniGames.Handlers;

[TelegramCommand("/start", "Show game menu")]
public class StartHandler(
    ITelegramMessageService messages,
    ITemplateRenderer renderer) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var text = renderer.Render("welcome", new Dictionary<string, string>(), "en");
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(text)
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/dice", "Roll the dice")]
public class DiceCommandHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("🎲 Rolling the dice... Send a dice sticker to play!")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/darts", "Throw darts")]
public class DartsCommandHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("🎯 Throw a dart! Send a dart emoji to play!")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/basketball", "Shoot hoops")]
public class BasketballCommandHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("🏀 Shoot some hoops! Send a basketball emoji to play!")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/bowling", "Bowl a strike")]
public class BowlingCommandHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("🎳 Try to bowl a strike! Send a bowling emoji to play!")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/football", "Kick the ball")]
public class FootballCommandHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("⚽ Score a goal! Send a football emoji to play!")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/slots", "Spin the slots")]
public class SlotsCommandHandler(ITelegramMessageService messages) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithText("🎰 Try your luck! Send a slot machine emoji to play!")
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}

[TelegramCommand("/leaderboard", "View top players")]
public class LeaderboardHandler(
    ITelegramMessageService messages,
    GameDbContext db) : ICommandHandler
{
    public async Task<CommandResult> HandleAsync(CommandContext context, CancellationToken ct)
    {
        var topPlayers = await db.PlayerScores
            .OrderByDescending(p => p.TotalScore)
            .Take(10)
            .ToListAsync(ct);

        if (topPlayers.Count == 0)
        {
            await messages.CreateMessage(context.BotId)
                .ToChat(context.ChatId!.Value)
                .WithText("🏆 No scores yet! Play some games first.")
                .SendAsync(ct);
            return CommandResult.Ok();
        }

        var lines = topPlayers.Select((p, i) =>
            $"{i + 1}. {p.UserName} — {p.TotalScore} pts ({p.GamesPlayed} games)");
        var text = "🏆 <b>Leaderboard</b>\n\n" + string.Join("\n", lines);

        await messages.CreateMessage(context.BotId)
            .ToChat(context.ChatId!.Value)
            .WithHtml(text)
            .SendAsync(ct);
        return CommandResult.Ok();
    }
}
