using BotForge.Core;
using Telegram.Bot.Types;

namespace BotForge.Routing.Handlers;

public interface IDiceHandler
{
    Task HandleAsync(DiceContext context, CancellationToken ct);
}

public class DiceContext(TelegramUpdateContext updateContext, Dice dice)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public Dice Dice { get; } = dice;

    /// <summary>Emoji that triggered the dice: 🎲, 🎯, 🏀, ⚽, 🎰, 🎳</summary>
    public string Emoji => Dice.Emoji;

    /// <summary>Value of the dice (1-6 for dice, 1-5 for darts, etc.)</summary>
    public int Value => Dice.Value;

    public long? ChatId => UpdateContext.ChatId;
    public long? UserId => UpdateContext.UserId;
    public string BotId => UpdateContext.BotId;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;
}
