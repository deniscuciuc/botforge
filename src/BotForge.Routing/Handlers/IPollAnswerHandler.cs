using BotForge.Core;
using Telegram.Bot.Types;

namespace BotForge.Routing.Handlers;

public interface IPollAnswerHandler
{
    Task HandleAsync(PollAnswerContext context, CancellationToken ct);
}

public class PollAnswerContext(TelegramUpdateContext updateContext, PollAnswer pollAnswer)
{
    public TelegramUpdateContext UpdateContext { get; } = updateContext;
    public PollAnswer PollAnswer { get; } = pollAnswer;
    public string PollId => PollAnswer.PollId;
    public int[] OptionIds => PollAnswer.OptionIds;
    public long? UserId => PollAnswer.User?.Id;
    public string BotId => UpdateContext.BotId;
    public CancellationToken CancellationToken => UpdateContext.CancellationToken;
    public IServiceProvider RequestServices => UpdateContext.RequestServices;
}
