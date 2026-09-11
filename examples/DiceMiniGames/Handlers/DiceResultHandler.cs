using DiceMiniGames.Data;
using Microsoft.EntityFrameworkCore;
using TeleForge.Messaging.Abstractions;
using TeleForge.Routing.Attributes;
using TeleForge.Routing.Handlers;

namespace DiceMiniGames.Handlers;

/// <summary>Handles all dice emoji results (🎲🎯🏀🎳⚽🎰).</summary>
[DiceMessage]
public class DiceResultHandler(
    ITelegramMessageService messages,
    GameDbContext db) : IDiceHandler
{
    private static readonly Dictionary<string, (int WinThreshold, int MaxPoints)> GameRules = new()
    {
        ["🎲"] = (4, 6), // dice: 1-6, win on 4+
        ["🎯"] = (4, 6), // darts: 1-6, win on 4+
        ["🏀"] = (4, 5), // basketball: 1-5, win on 4+
        ["⚽"] = (3, 5), // football: 1-5, win on 3+
        ["🎳"] = (4, 6), // bowling: 1-6, win on 4+
        ["🎰"] = (22, 64) // slots: 1-64, jackpot on 22+
    };

    public async Task HandleAsync(DiceContext context, CancellationToken ct)
    {
        var emoji = context.Emoji;
        var value = context.Value;
        var userId = context.UserId ?? 0;
        var chatId = context.ChatId ?? 0;

        if (!GameRules.TryGetValue(emoji, out var rules))
            return;

        var won = value >= rules.WinThreshold;
        var points = won ? value * 10 : 0;
        var userName = context.UpdateContext.RawUpdate.Message?.From?.FirstName ?? "Player";

        // Record game result
        db.GameResults.Add(new GameResult
        {
            UserId = userId,
            GameType = emoji,
            DiceValue = value,
            PointsEarned = points
        });

        // Update player score
        var score = await db.PlayerScores
            .FirstOrDefaultAsync(p => p.UserId == userId && p.GameType == emoji, ct);

        if (score == null)
        {
            score = new PlayerScore
            {
                UserId = userId,
                UserName = userName,
                GameType = emoji,
                TotalScore = points,
                GamesPlayed = 1,
                BestResult = value,
                LastPlayed = DateTime.UtcNow
            };
            db.PlayerScores.Add(score);
        }
        else
        {
            score.TotalScore += points;
            score.GamesPlayed++;
            score.BestResult = Math.Max(score.BestResult, value);
            score.LastPlayed = DateTime.UtcNow;
            score.UserName = userName;
        }

        await db.SaveChangesAsync(ct);

        // Respond with result
        var resultText = won
            ? $"{emoji} You got *{value}*!\n\n🎉 You won! +{points} points\n\nYour total: *{score.TotalScore}* points"
            : $"{emoji} You got *{value}*!\n\nBetter luck next time!\n\nYour total: *{score.TotalScore}* points";

        // Delay response slightly so user sees the dice animation
        await Task.Delay(3000, ct);

        await messages.CreateMessage(context.BotId)
            .ToChat(chatId)
            .WithText(resultText)
            .SendAsync(ct);
    }
}
